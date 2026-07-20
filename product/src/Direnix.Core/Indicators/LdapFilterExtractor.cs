using System.Text.RegularExpressions;

namespace Direnix.Core.Indicators;

/// <summary>
/// Traduz a consulta que o usuario digitou (LDAP ou PowerShell) num filtro LDAP
/// read-only que o coletor executa. PowerShell aqui e apenas conforto de digitacao:
/// NUNCA e executado — extraimos/traduzimos o filtro e rodamos como busca LDAP.
/// Cobertura honesta: <c>-LDAPFilter</c> integral + um subconjunto comum de
/// <c>-Filter</c>; o que nao der para traduzir retorna erro pedindo o -LDAPFilter.
/// </summary>
public static partial class LdapFilterExtractor
{
    public static bool TryBuild(string? kind, string? query, out string ldapFilter, out string? error)
    {
        ldapFilter = string.Empty;
        error = null;

        if (string.IsNullOrWhiteSpace(query))
        {
            error = "Consulta vazia.";
            return false;
        }

        var isPowerShell = string.Equals(kind, "PowerShell", StringComparison.OrdinalIgnoreCase);
        var raw = isPowerShell ? ExtractFromPowerShell(query, out error) : query.Trim();
        if (raw is null)
        {
            return false;
        }

        return TryNormalizeLdap(raw, out ldapFilter, out error);
    }

    /// <summary>
    /// Extrai um filtro LDAP de uma linha PowerShell (Get-AD* ...). Suporta as formas
    /// comuns: -LDAPFilter (verbatim), -Filter (subconjunto traduzido), -Identity
    /// (localiza o objeto) e o cmdlet sem filtro (todos do tipo). O cmdlet
    /// (Get-ADUser/Computer/Group/OU) define a categoria de objeto.
    /// </summary>
    private static string? ExtractFromPowerShell(string command, out string? error)
    {
        error = null;

        // 1. -LDAPFilter tem prioridade e e usado integralmente (o usuario ja deu LDAP).
        var ldap = LdapFilterArg().Match(command);
        if (ldap.Success)
        {
            return ldap.Groups["v"].Value;
        }

        // Categoria/campos de identidade a partir do cmdlet (Get-ADUser/Computer/...).
        var (category, idFields, isComputer) = CmdletShape(command);

        // 2. -Filter "<expr>" ou -Filter * (combina com a categoria do cmdlet).
        var filter = PsFilterArg().Match(command);
        if (filter.Success)
        {
            var expr = filter.Groups["v"].Value.Trim();
            if (expr is "*")
            {
                return category is null ? "(objectClass=*)" : Combine(category, null);
            }

            var inner = TranslateAdFilter(expr, out error);
            return inner is null ? null : Combine(category, inner);
        }

        // 3. -Identity <valor>: localiza o objeto por sam/nome/DN/dNSHostName.
        var identity = IdentityArg().Match(command);
        if (identity.Success)
        {
            var value = identity.Groups["v"].Value.Trim().Trim('\'', '"');
            return Combine(category, BuildIdentityFilter(value, idFields, isComputer));
        }

        // 4. Cmdlet sem filtro/identity (ex.: "Get-ADComputer"): todos do tipo.
        if (category is not null)
        {
            return Combine(category, null);
        }

        error = "Nao reconheci a consulta PowerShell. Use um comando Get-AD* com -Filter, -LDAPFilter ou -Identity.";
        return null;
    }

    /// <summary>Categoria LDAP (clausulas sem o & externo) + campos de identidade por cmdlet.</summary>
    private static (string? Category, string[] IdFields, bool IsComputer) CmdletShape(string command)
    {
        var noun = CmdletArg().Match(command) is { Success: true } m ? m.Groups["n"].Value.ToLowerInvariant() : string.Empty;
        return noun switch
        {
            "user" => ("(objectCategory=person)(objectClass=user)",
                ["sAMAccountName", "userPrincipalName", "name", "cn", "distinguishedName"], false),
            "computer" => ("(objectCategory=computer)",
                ["sAMAccountName", "name", "cn", "dNSHostName", "distinguishedName"], true),
            "group" => ("(objectCategory=group)",
                ["sAMAccountName", "name", "cn", "distinguishedName"], false),
            "organizationalunit" => ("(objectCategory=organizationalUnit)",
                ["name", "ou", "distinguishedName"], false),
            _ => (null, ["name", "cn", "distinguishedName"], false)
        };
    }

    /// <summary>Filtro que localiza um objeto por -Identity (sam/nome/DN/dNSHostName; computador tenta tambem sam$).</summary>
    private static string BuildIdentityFilter(string value, string[] fields, bool isComputer)
    {
        var alts = fields.Select(f => $"({f}={value})").ToList();
        if (isComputer)
        {
            alts.Add($"(sAMAccountName={value}$)");
        }

        return alts.Count == 1 ? alts[0] : $"(|{string.Concat(alts)})";
    }

    /// <summary>Combina as clausulas de categoria com uma clausula opcional, com AND quando necessario.</summary>
    private static string Combine(string? categoryClauses, string? clause)
    {
        var joined = (categoryClauses ?? string.Empty) + (clause ?? string.Empty);
        return TopLevelCount(joined) <= 1 ? joined : $"(&{joined})";
    }

    /// <summary>Conta grupos "(...)" no nivel superior de uma string de clausulas.</summary>
    private static int TopLevelCount(string clauses)
    {
        int count = 0, depth = 0;
        foreach (var ch in clauses)
        {
            if (ch == '(')
            {
                if (depth == 0)
                {
                    count++;
                }

                depth++;
            }
            else if (ch == ')')
            {
                depth--;
            }
        }

        return count;
    }

    /// <summary>Traduz um subconjunto do -Filter do modulo ActiveDirectory para LDAP.</summary>
    private static string? TranslateAdFilter(string expr, out string? error)
    {
        error = null;

        var hasAnd = AndSplit().IsMatch(expr);
        var hasOr = OrSplit().IsMatch(expr);
        if (hasAnd && hasOr)
        {
            error = "Combine as condicoes com apenas -and OU apenas -or (sem misturar). Para casos complexos use -LDAPFilter.";
            return null;
        }

        var parts = (hasOr ? OrSplit() : AndSplit()).Split(expr);
        var clauses = new List<string>();
        foreach (var part in parts)
        {
            var clause = TranslateComparison(part.Trim(), out error);
            if (clause is null)
            {
                return null;
            }

            clauses.Add(clause);
        }

        if (clauses.Count == 1)
        {
            return clauses[0];
        }

        return $"({(hasOr ? '|' : '&')}{string.Concat(clauses)})";
    }

    private static string? TranslateComparison(string comparison, out string? error)
    {
        error = null;
        var m = Comparison().Match(comparison);
        if (!m.Success)
        {
            error = $"Nao consegui traduzir a condicao '{comparison}'. Use -LDAPFilter para casos avancados.";
            return null;
        }

        var prop = m.Groups["p"].Value.Trim();
        var op = m.Groups["o"].Value.Trim().ToLowerInvariant();
        var value = m.Groups["v"].Value.Trim().Trim('\'', '"');

        // Enabled e uma propriedade sintetica do modulo AD -> bit ACCOUNTDISABLE do UAC.
        if (string.Equals(prop, "Enabled", StringComparison.OrdinalIgnoreCase))
        {
            var wantsEnabled = value.Equals("$true", StringComparison.OrdinalIgnoreCase);
            if (op is "-ne")
            {
                wantsEnabled = !wantsEnabled;
            }

            const string disabledBit = "(userAccountControl:1.2.840.113556.1.4.803:=2)";
            return wantsEnabled ? $"(!{disabledBit})" : disabledBit;
        }

        return op switch
        {
            "-eq" or "-like" => $"({prop}={value})",
            "-ne" or "-notlike" => $"(!({prop}={value}))",
            "-ge" => $"({prop}>={value})",
            "-le" => $"({prop}<={value})",
            "-gt" => $"(!({prop}<={value}))",
            "-lt" => $"(!({prop}>={value}))",
            _ => Fail(out error, $"Operador '{op}' nao suportado. Use -LDAPFilter.")
        };
    }

    private static string? Fail(out string? error, string message)
    {
        error = message;
        return null;
    }

    /// <summary>Normaliza/valida um filtro LDAP: parenteses balanceados, delimitado por (...).</summary>
    private static bool TryNormalizeLdap(string raw, out string ldapFilter, out string? error)
    {
        ldapFilter = raw.Trim();
        error = null;

        if (ldapFilter.Length == 0)
        {
            error = "Filtro LDAP vazio.";
            return false;
        }

        // Permite o usuario digitar "attr=valor" sem parenteses externos.
        if (!ldapFilter.StartsWith('(') && ldapFilter.Contains('='))
        {
            ldapFilter = $"({ldapFilter})";
        }

        if (!ldapFilter.StartsWith('(') || !ldapFilter.EndsWith(')'))
        {
            error = "Filtro LDAP deve comecar com '(' e terminar com ')'.";
            return false;
        }

        var depth = 0;
        foreach (var ch in ldapFilter)
        {
            if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')')
            {
                depth--;
                if (depth < 0)
                {
                    error = "Parenteses desbalanceados no filtro LDAP.";
                    return false;
                }
            }
        }

        if (depth != 0)
        {
            error = "Parenteses desbalanceados no filtro LDAP.";
            return false;
        }

        return true;
    }

    [GeneratedRegex(@"-LDAPFilter\s+(['""])(?<v>.+?)\1", RegexOptions.IgnoreCase)]
    private static partial Regex LdapFilterArg();

    [GeneratedRegex(@"-Filter\s+(?:(['""])(?<v>.+?)\1|(?<v>\*))", RegexOptions.IgnoreCase)]
    private static partial Regex PsFilterArg();

    [GeneratedRegex(@"-Identity\s+(?:(['""])(?<v>.+?)\1|(?<v>[^\s]+))", RegexOptions.IgnoreCase)]
    private static partial Regex IdentityArg();

    [GeneratedRegex(@"Get-AD(?<n>[A-Za-z]+)", RegexOptions.IgnoreCase)]
    private static partial Regex CmdletArg();

    [GeneratedRegex(@"\s+-and\s+", RegexOptions.IgnoreCase)]
    private static partial Regex AndSplit();

    [GeneratedRegex(@"\s+-or\s+", RegexOptions.IgnoreCase)]
    private static partial Regex OrSplit();

    [GeneratedRegex(@"^(?<p>[\w-]+)\s+(?<o>-\w+)\s+(?<v>.+)$")]
    private static partial Regex Comparison();
}
