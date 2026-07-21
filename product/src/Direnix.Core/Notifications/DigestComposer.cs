using System.Globalization;
using System.Text;
using System.Text.Json;
using Direnix.Core.Reporting;

namespace Direnix.Core.Notifications;

/// <summary>
/// Monta a mensagem de digest (assunto + HTML + texto + JSON) a partir do mesmo
/// <see cref="ReportModel"/> do relatório. Aplica a política de envio.
/// </summary>
public static class DigestComposer
{
    private static readonly Dictionary<string, Dictionary<string, string>> Strings = new()
    {
        ["pt"] = new()
        {
            ["subjectNoActivity"] = "Direnix — {0}: sem novidades",
            ["subjectActivity"] = "Direnix — {0}: {1} novo(s) risco(s), {2} mudança(s)",
            ["subjectNoDomain"] = "Direnix — ronda diária",
            ["greeting"] = "Ronda diária de identidade",
            ["newRisks"] = "Novos riscos (24h)",
            ["changes"] = "Mudanças (24h)",
            ["identityScore"] = "Identity Score",
            ["activeRisks"] = "Riscos ativos",
            ["openPortal"] = "Abrir portal",
            ["noActivity"] = "Nenhuma mudança ou novo risco no período."
        },
        ["en"] = new()
        {
            ["subjectNoActivity"] = "Direnix — {0}: all quiet",
            ["subjectActivity"] = "Direnix — {0}: {1} new risk(s), {2} change(s)",
            ["subjectNoDomain"] = "Direnix — daily rounds",
            ["greeting"] = "Daily identity rounds",
            ["newRisks"] = "New risks (24h)",
            ["changes"] = "Changes (24h)",
            ["identityScore"] = "Identity Score",
            ["activeRisks"] = "Active risks",
            ["openPortal"] = "Open portal",
            ["noActivity"] = "No changes or new risks in the period."
        }
    };

    /// <summary>Há atividade relevante para reportar (base da política OnlyWhenActivity).</summary>
    public static bool HasActivity(ReportModel model) =>
        model.NewFindings24H > 0
        || model.ChangeSummary24H.Any(c => c.Count > 0)
        || model.Indicators.Any(i => i.Count > 0);

    /// <summary>
    /// Decide se deve enviar segundo a política. Retorna a mensagem pronta, ou null
    /// quando a política é OnlyWhenActivity e não houve atividade.
    /// </summary>
    public static DigestMessage? Compose(ReportModel model, DigestPolicy policy, string lang)
    {
        if (policy == DigestPolicy.OnlyWhenActivity && !HasActivity(model))
        {
            return null;
        }

        return Build(model, lang);
    }

    /// <summary>Monta a mensagem sempre (usado pelo envio de teste).</summary>
    public static DigestMessage Build(ReportModel model, string lang)
    {
        var l = Strings.ContainsKey(lang) ? lang : "pt";
        string T(string key) => Strings[l].TryGetValue(key, out var v) ? v : key;

        var totalChanges = model.ChangeSummary24H.Sum(c => c.Count);
        var domain = string.IsNullOrWhiteSpace(model.DomainName) ? null : model.DomainName;

        string subject = domain is null
            ? T("subjectNoDomain")
            : HasActivity(model)
                ? string.Format(CultureInfo.InvariantCulture, T("subjectActivity"), domain, model.NewFindings24H, totalChanges)
                : string.Format(CultureInfo.InvariantCulture, T("subjectNoActivity"), domain);

        var html = ReportBuilder.BuildDigestHtml(model, l);
        var text = BuildText(model, l, T);
        var json = BuildJson(model, totalChanges);

        return new DigestMessage(subject, html, text, json);
    }

    private static string BuildText(ReportModel model, string lang, Func<string, string> t)
    {
        var sb = new StringBuilder();
        sb.Append("Direnix — ").AppendLine(t("greeting"));
        if (!string.IsNullOrWhiteSpace(model.DomainName))
        {
            sb.AppendLine(model.DomainName);
        }
        sb.AppendLine();

        if (!DigestComposer.HasActivity(model))
        {
            sb.AppendLine(t("noActivity"));
        }
        else
        {
            sb.Append(t("newRisks")).Append(": ").AppendLine(model.NewFindings24H.ToString(CultureInfo.InvariantCulture));
            sb.Append(t("changes")).Append(": ").AppendLine(model.ChangeSummary24H.Sum(c => c.Count).ToString(CultureInfo.InvariantCulture));
            foreach (var indicator in model.Indicators.Where(i => i.Count > 0))
            {
                sb.Append("  • ").Append(indicator.Title).Append(": ").AppendLine(indicator.Count.ToString(CultureInfo.InvariantCulture));
            }
        }

        sb.Append(t("identityScore")).Append(": ").AppendLine(model.IdentityScore.ToString(CultureInfo.InvariantCulture));
        sb.Append(t("activeRisks")).Append(": ").AppendLine(model.ActiveFindings.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine();
        sb.Append(t("openPortal")).Append(": ").AppendLine(model.PortalUrl);
        return sb.ToString();
    }

    private static string BuildJson(ReportModel model, int totalChanges)
    {
        var payload = new
        {
            product = "Direnix",
            version = model.ProductVersion,
            generatedAt = model.GeneratedAt,
            domain = model.DomainName,
            portalUrl = model.PortalUrl,
            identityScore = model.IdentityScore,
            tier0Score = model.Tier0Score,
            activeFindings = model.ActiveFindings,
            newFindings24h = model.NewFindings24H,
            totalChanges24h = totalChanges,
            changes = model.ChangeSummary24H.Select(c => new { type = c.ChangeType, count = c.Count }),
            indicators = model.Indicators.Select(i => new { title = i.Title, count = i.Count }),
            topRisks = model.TopFindings.Take(10).Select(f => new
            {
                severity = f.Severity.ToString(),
                category = f.Category.ToString(),
                title = f.Title,
                @object = f.ObjectDisplay
            })
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = false });
    }
}
