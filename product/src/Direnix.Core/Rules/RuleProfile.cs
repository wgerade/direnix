namespace Direnix.Core.Rules;

/// <summary>
/// Perfil de regras nomeado: thresholds + regras desabilitadas. Perfis built-in
/// sao somente leitura; o usuario pode "Salvar como" para criar perfis proprios.
/// </summary>
public sealed record RuleProfile(
    string Name,
    bool BuiltIn,
    RuleThresholds Thresholds,
    IReadOnlyList<string> DisabledRules)
{
    public bool IsRuleEnabled(string ruleId) =>
        !DisabledRules.Contains(ruleId, StringComparer.OrdinalIgnoreCase);

    /// <summary>Indicadores built-in desabilitados neste perfil (por id).</summary>
    public IReadOnlyList<string> DisabledIndicators { get; init; } = [];

    public bool IsIndicatorEnabled(string indicatorId) =>
        !DisabledIndicators.Contains(indicatorId, StringComparer.OrdinalIgnoreCase);

    /// <summary>Horizonte (dias) do indicador "senha vencendo" e "conta a expirar".</summary>
    public int IndicatorHorizonDays { get; init; } = 7;

    /// <summary>
    /// Indicadores customizados definidos pelo usuario; rodam junto da coleta deste
    /// perfil (inclusive na coleta agendada), como buscas LDAP read-only.
    /// </summary>
    public IReadOnlyList<CustomIndicatorDef> CustomIndicators { get; init; } = [];
}

/// <summary>
/// Definicao de um indicador customizado no perfil. O usuario digita a consulta em
/// LDAP ou PowerShell (conforto); o sistema traduz para filtro LDAP e executa
/// read-only. <see cref="Kind"/>: "Ldap" ou "PowerShell".
/// </summary>
public sealed record CustomIndicatorDef(
    string Id,
    string Name,
    string Kind,
    string Query,
    string ObjectType,
    bool Enabled);

/// <summary>Estado completo de perfis: o ativo + a lista (built-in + customizados).</summary>
public sealed record RuleProfilesState(string ActiveProfile, IReadOnlyList<RuleProfile> Profiles)
{
    public RuleProfile ResolveActive()
    {
        var match = Profiles.FirstOrDefault(p => string.Equals(p.Name, ActiveProfile, StringComparison.OrdinalIgnoreCase));
        return match ?? Profiles.FirstOrDefault() ?? BuiltInProfiles.MicrosoftDefault;
    }
}

/// <summary>Perfis padrao de mercado (somente leitura).</summary>
public static class BuiltInProfiles
{
    public static RuleProfile MicrosoftDefault { get; } = new(
        "MicrosoftDefault", true,
        new RuleThresholds(),
        []);

    public static RuleProfile CisStrict { get; } = new(
        "CISStrict", true,
        new RuleThresholds
        {
            StaleUserDays = 45,
            StaleComputerDays = 45,
            DisabledObjectRetentionDays = 90,
            DormantSensitiveEntityDays = 90,
            KrbtgtRotationDays = 90,
            MachineAccountQuotaExpected = 0
        },
        []);

    public static RuleProfile OperationalBalanced { get; } = new(
        "OperationalBalanced", true,
        new RuleThresholds
        {
            StaleUserDays = 120,
            StaleComputerDays = 120,
            DisabledObjectRetentionDays = 365,
            DormantSensitiveEntityDays = 180,
            KrbtgtRotationDays = 180,
            MachineAccountQuotaExpected = 0
        },
        []);

    public static IReadOnlyList<RuleProfile> All => [MicrosoftDefault, CisStrict, OperationalBalanced];

    /// <summary>Garante que os built-in existam no estado e que haja um ativo valido.</summary>
    public static RuleProfilesState Normalize(RuleProfilesState? state)
    {
        var profiles = new List<RuleProfile>(All);
        if (state is not null)
        {
            foreach (var profile in state.Profiles)
            {
                if (!profile.BuiltIn && !profiles.Any(p => string.Equals(p.Name, profile.Name, StringComparison.OrdinalIgnoreCase)))
                {
                    profiles.Add(profile with { BuiltIn = false });
                }
            }
        }

        var active = state?.ActiveProfile;
        if (string.IsNullOrWhiteSpace(active) || !profiles.Any(p => string.Equals(p.Name, active, StringComparison.OrdinalIgnoreCase)))
        {
            active = MicrosoftDefault.Name;
        }

        return new RuleProfilesState(active, profiles);
    }
}
