using Direnix.Core.Changes;
using Direnix.Core.Collection;
using Direnix.Core.Findings;
using Xunit;
using static Direnix.Core.Tests.TestData;

namespace Direnix.Core.Tests;

public class ChangeDetectorTests
{
    private static Dictionary<string, IReadOnlyList<string>> Prev(params (string Key, string[] Values)[] attrs)
    {
        var dict = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (k, v) in attrs) dict[k] = v;
        return dict;
    }

    [Fact]
    public void MemberAddedToPrivilegedGroup_IsCritical()
    {
        var group = Make(AdObjectType.Group, "CN=Domain Admins,DC=corp,DC=local", "Domain Admins", "S-1-5-21-1-2-3-512",
            ("member", ["CN=joe,DC=corp,DC=local"]));
        var prev = Prev(("member", Array.Empty<string>()));

        var change = Assert.Single(ChangeDetector.Detect(group, prev));
        Assert.Equal(ChangeType.PrivilegedMemberAdded, change.Type);
        Assert.Equal(Severity.Critical, change.Severity);
        Assert.Equal("CN=joe,DC=corp,DC=local", change.NewValue);
    }

    [Fact]
    public void MemberAddedToNormalGroup_IsInfo()
    {
        var group = Make(AdObjectType.Group, "CN=VPN,DC=corp,DC=local", "VPN", "S-1-5-21-1-2-3-1500",
            ("member", ["CN=ana,DC=corp,DC=local"]));
        var change = Assert.Single(ChangeDetector.Detect(group, Prev()));
        Assert.Equal(ChangeType.MemberAdded, change.Type);
        Assert.Equal(Severity.Info, change.Severity);
    }

    [Fact]
    public void AccountReEnabled_IsDetected()
    {
        var user = Make(AdObjectType.User, "CN=u,DC=corp,DC=local", "u", "S-1-5-21-1-2-3-1600",
            Attr("userAccountControl", "512"));
        var prev = Prev(("userAccountControl", ["514"])); // disabled antes

        Assert.Contains(ChangeDetector.Detect(user, prev), c => c.Type == ChangeType.AccountEnabled);
    }

    [Fact]
    public void DangerousFlagSet_IsHigh()
    {
        var user = Make(AdObjectType.User, "CN=u,DC=corp,DC=local", "u", "S-1-5-21-1-2-3-1601",
            Attr("userAccountControl", "4194816")); // 512 + DONT_REQ_PREAUTH
        var prev = Prev(("userAccountControl", ["512"]));

        var flag = Assert.Single(ChangeDetector.Detect(user, prev), c => c.Type == ChangeType.DangerousFlagSet);
        Assert.Equal(Severity.High, flag.Severity);
        Assert.Equal("DontRequirePreauth", flag.Attribute);
    }

    [Fact]
    public void SpnAdded_IsDetected()
    {
        var user = Make(AdObjectType.User, "CN=svc,DC=corp,DC=local", "svc", "S-1-5-21-1-2-3-1602",
            ("servicePrincipalName", ["MSSQLSvc/db01"]));
        var change = Assert.Single(ChangeDetector.Detect(user, Prev()), c => c.Type == ChangeType.SpnAdded);
        Assert.Equal("MSSQLSvc/db01", change.NewValue);
    }

    [Fact]
    public void NoChange_ProducesNothing()
    {
        var user = Make(AdObjectType.User, "CN=u,DC=corp,DC=local", "u", "S-1-5-21-1-2-3-1603",
            Attr("userAccountControl", "512"), Attr("description", "same"));
        var prev = Prev(("userAccountControl", ["512"]), ("description", ["same"]));

        Assert.Empty(ChangeDetector.Detect(user, prev));
    }
}
