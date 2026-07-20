namespace Direnix.Core.Indicators;

/// <summary>
/// Item (objeto) que compoe um indicador operacional, para o drill-down: nome
/// amigavel, DN, SID e um detalhe contextual (ex.: data de expiracao / bloqueio).
/// </summary>
public sealed record IndicatorItem(
    string Display,
    string DistinguishedName,
    string? ObjectSid,
    string? Detail);

/// <summary>
/// Resultado de um indicador num run: contagem + amostra de objetos. Diferente de
/// um "risco" (Finding): nao tem severidade nem remediacao — e informacao
/// operacional que o administrador acompanha no dia a dia.
/// </summary>
public sealed record IndicatorResult(
    string Id,
    string Title,
    string Category,
    bool IsCustom,
    int Count,
    IReadOnlyList<IndicatorItem> Items);

/// <summary>Metadado estatico de um indicador built-in.</summary>
public sealed record IndicatorDefinition(
    string Id,
    string Title,
    string Category,
    string Description);

/// <summary>Catalogo dos indicadores operacionais built-in.</summary>
public static class IndicatorCatalog
{
    public const string PasswordExpiring = "IND-PWD-EXPIRING";
    public const string PasswordExpired = "IND-PWD-EXPIRED";
    public const string AccountLocked = "IND-ACCT-LOCKED";
    public const string AccountExpiring = "IND-ACCT-EXPIRING";

    /// <summary>Teto de itens guardados por indicador (a contagem continua exata).</summary>
    public const int MaxItems = 1000;

    public static IReadOnlyList<IndicatorDefinition> All { get; } =
    [
        new(PasswordExpiring, "Senhas vencendo", "Senha",
            "Contas habilitadas cuja senha expira dentro do horizonte configurado (inclui 'vence hoje')."),
        new(PasswordExpired, "Senhas expiradas", "Senha",
            "Contas habilitadas com a senha ja expirada (ou marcada para trocar no proximo logon)."),
        new(AccountLocked, "Contas bloqueadas", "Conta",
            "Contas atualmente bloqueadas por excesso de tentativas (considera a duracao de bloqueio do dominio)."),
        new(AccountExpiring, "Contas a expirar", "Conta",
            "Contas habilitadas cujo accountExpires ocorre dentro do horizonte configurado."),
    ];
}
