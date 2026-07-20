using System.DirectoryServices.Protocols;
using System.Net;
using Direnix.Core.Collection;
using DirectoryProtocol = Direnix.Core.Collection.DirectoryProtocol;

namespace Direnix.Infrastructure.Directory;

public sealed class LdapDirectoryProbe : IAdDirectoryProbe
{
    private static readonly string[] RootDseAttributes =
    [
        "defaultNamingContext",
        "configurationNamingContext",
        "schemaNamingContext",
        "rootDomainNamingContext"
    ];

    public Task<DirectoryProbeResult> ProbeRootDseAsync(
        DirectoryTarget target,
        CancellationToken cancellationToken)
    {
        return Task.Run(() => ProbeRootDse(target, cancellationToken), cancellationToken);
    }

    private static DirectoryProbeResult ProbeRootDse(
        DirectoryTarget target,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = new List<string>();
        var errors = new List<string>();

        if (target.Protocol == DirectoryProtocol.Ldap)
        {
            warnings.Add("LDAP without TLS is allowed only as an explicit lab exception.");
        }

        try
        {
            var identifier = new LdapDirectoryIdentifier(target.Host, target.Port, true, false);

            using var connection = new LdapConnection(identifier)
            {
                AuthType = AuthType.Negotiate,
                Timeout = target.Timeout
            };

            connection.SessionOptions.ProtocolVersion = 3;
            connection.SessionOptions.SecureSocketLayer = target.Protocol == DirectoryProtocol.Ldaps;

            if (target.Credential is not null)
            {
                connection.Credential = new NetworkCredential(
                    target.Credential.UserName,
                    target.Credential.Secret,
                    target.Credential.Domain);
            }

            var request = new SearchRequest(
                string.Empty,
                "(objectClass=*)",
                SearchScope.Base,
                RootDseAttributes);

            var response = (SearchResponse)connection.SendRequest(request, target.Timeout);
            var entry = response.Entries.Count > 0 ? response.Entries[0] : null;

            if (entry is null)
            {
                errors.Add("RootDSE returned no entries.");
                return BuildResult(target, CapabilityState.Blocked, warnings, errors, null);
            }

            var namingContexts = new DirectoryNamingContexts(
                ReadAttribute(entry, "defaultNamingContext"),
                ReadAttribute(entry, "configurationNamingContext"),
                ReadAttribute(entry, "schemaNamingContext"),
                ReadAttribute(entry, "rootDomainNamingContext"));

            var state = warnings.Count == 0 ? CapabilityState.Ready : CapabilityState.ReadyWithWarnings;
            return BuildResult(target, state, warnings, errors, namingContexts);
        }
        catch (Exception ex) when (ex is LdapException or DirectoryOperationException or InvalidOperationException)
        {
            errors.Add(ex.Message);
            return BuildResult(target, CapabilityState.Blocked, warnings, errors, null);
        }
    }

    private static DirectoryProbeResult BuildResult(
        DirectoryTarget target,
        CapabilityState state,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors,
        DirectoryNamingContexts? namingContexts)
    {
        return new DirectoryProbeResult(
            state,
            target.Host,
            target.Protocol,
            target.Port,
            namingContexts ?? new DirectoryNamingContexts(null, null, null, null),
            warnings,
            errors);
    }

    private static string? ReadAttribute(SearchResultEntry entry, string name)
    {
        if (!entry.Attributes.Contains(name))
        {
            return null;
        }

        var attribute = entry.Attributes[name];
        if (attribute is null || attribute.Count == 0)
        {
            return null;
        }

        return attribute[0]?.ToString();
    }
}
