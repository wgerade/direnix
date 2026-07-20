using Direnix.Core.Collection;

namespace Direnix.Core.Tests;

internal static class TestData
{
    public static string FileTime(DateTimeOffset value) =>
        value.ToFileTime().ToString();

    public static CollectedObject Make(
        AdObjectType type,
        string dn,
        string? sam,
        string? sid,
        params (string Key, string[] Values)[] attributes)
    {
        var dict = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in attributes)
        {
            dict[key] = values;
        }

        return new CollectedObject(type, dn, dn, sid, sam, dict);
    }

    public static (string, string[]) Attr(string key, string value) => (key, [value]);

    /// <summary>Cria uma GPO com objectGUID explícito (DN = CN={guid},CN=Policies,...).</summary>
    public static CollectedObject MakeGpo(string guid, params (string Key, string[] Values)[] attributes)
    {
        var dict = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, values) in attributes)
        {
            dict[key] = values;
        }

        var dn = $"CN={{{guid}}},CN=Policies,CN=System,DC=corp,DC=local";
        return new CollectedObject(AdObjectType.GroupPolicyContainer, dn, guid, null, null, dict);
    }
}
