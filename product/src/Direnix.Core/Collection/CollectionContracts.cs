namespace Direnix.Core.Collection;

public enum DirectoryProtocol
{
    Ldaps = 636,
    Ldap = 389
}

public enum CollectionDepth
{
    Quick,
    Standard,
    Deep
}

public enum CapabilityState
{
    Ready,
    ReadyWithWarnings,
    Blocked,
    CapabilityMissing,
    NeedsEvidence
}

public sealed record DirectoryCredential(string UserName, string? Domain, string Secret)
{
    public string Principal => string.IsNullOrWhiteSpace(Domain) ? UserName : $"{Domain}\\{UserName}";

    public override string ToString() => Principal;
}

public sealed record DirectoryTarget(
    string Host,
    DirectoryProtocol Protocol,
    int Port,
    DirectoryCredential? Credential,
    TimeSpan Timeout)
{
    public static DirectoryTarget DefaultLdaps(string host) =>
        new(host, DirectoryProtocol.Ldaps, 636, null, TimeSpan.FromSeconds(15));
}

public sealed record CollectionScope(
    string? DomainDn,
    IReadOnlyList<string> SearchBase,
    IReadOnlyList<string> ObjectTypes,
    IReadOnlyList<string> FeaturePacks,
    CollectionDepth Depth);

public sealed record DirectoryNamingContexts(
    string? DefaultNamingContext,
    string? ConfigurationNamingContext,
    string? SchemaNamingContext,
    string? RootDomainNamingContext);

public sealed record DirectoryProbeResult(
    CapabilityState State,
    string Host,
    DirectoryProtocol Protocol,
    int Port,
    DirectoryNamingContexts NamingContexts,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Errors);
