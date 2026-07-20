using Direnix.Core.Collection;
using Direnix.Core.Indicators;
using Direnix.Core.Rules;
using Xunit;
using static Direnix.Core.Tests.TestData;

namespace Direnix.Core.Tests;

public class IndicatorsTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static CollectionResult Result(IEnumerable<CollectedObject> objects, IEnumerable<CustomIndicatorMatch>? custom = null) =>
        new(
            "run-1", "run-1", "Scoped", CoverageMode.StandardOrFull, Now.AddMinutes(-1), Now,
            "DC=corp,DC=local", "corp.local",
            new DirectoryNamingContexts(null, null, null, null),
            objects.ToList(), Array.Empty<ObjectTypeOutcome>(), Array.Empty<string>(),
            CollectionDepth.Standard, "test", Array.Empty<string>(), Array.Empty<string>())
        {
            CustomIndicatorMatches = (custom ?? Array.Empty<CustomIndicatorMatch>()).ToList()
        };

    private static CollectedObject User(string sam, params (string Key, string[] Values)[] attrs) =>
        Make(AdObjectType.User, $"CN={sam},DC=corp,DC=local", sam, "S-1-5-21-1-2-3-1200", attrs);

    private static IndicatorResult? Find(CollectionResult result, string id, RuleProfile? profile = null) =>
        IndicatorEngine.Evaluate(result, profile ?? new RuleProfile("P", false, new RuleThresholds(), []))
            .FirstOrDefault(i => i.Id == id);

    [Fact]
    public void PasswordExpiring_FlagsUserWithinHorizon_NotNeverOrExpired()
    {
        var soon = User("soon", Attr("userAccountControl", "512"),
            Attr("msDS-UserPasswordExpiryTimeComputed", FileTime(Now.AddDays(3))));
        var far = User("far", Attr("userAccountControl", "512"),
            Attr("msDS-UserPasswordExpiryTimeComputed", FileTime(Now.AddDays(60))));
        var never = User("never", Attr("userAccountControl", "512"),
            Attr("msDS-UserPasswordExpiryTimeComputed", long.MaxValue.ToString()));

        var result = Find(Result([soon, far, never]), IndicatorCatalog.PasswordExpiring);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
        Assert.Equal("soon", result.Items[0].Display);
    }

    [Fact]
    public void PasswordExpiring_ExcludesDisabledManagedAndNeverExpire()
    {
        var disabled = User("disabled", Attr("userAccountControl", "514"),
            Attr("msDS-UserPasswordExpiryTimeComputed", FileTime(Now.AddDays(2))));
        var neverFlag = User("neverflag", Attr("userAccountControl", "66048"), // 512 + DONT_EXPIRE_PASSWORD
            Attr("msDS-UserPasswordExpiryTimeComputed", FileTime(Now.AddDays(2))));
        var managed = Make(AdObjectType.User, "CN=svc$,DC=corp,DC=local", "svc$", "S-1-5-21-1-2-3-1300",
            Attr("userAccountControl", "512"),
            Attr("msDS-UserPasswordExpiryTimeComputed", FileTime(Now.AddDays(2))));

        var result = Find(Result([disabled, neverFlag, managed]), IndicatorCatalog.PasswordExpiring);

        Assert.NotNull(result);
        Assert.Equal(0, result!.Count);
    }

    [Fact]
    public void PasswordExpired_FlagsPastAndMustChange()
    {
        var expired = User("expired", Attr("userAccountControl", "512"),
            Attr("msDS-UserPasswordExpiryTimeComputed", FileTime(Now.AddDays(-1))));
        var mustChange = User("mustchange", Attr("userAccountControl", "512"),
            Attr("msDS-UserPasswordExpiryTimeComputed", "0"));

        var result = Find(Result([expired, mustChange]), IndicatorCatalog.PasswordExpired);

        Assert.NotNull(result);
        Assert.Equal(2, result!.Count);
    }

    [Fact]
    public void AccountLocked_FlagsWhenLockoutTimeWithinDuration()
    {
        // lockoutDuration do dominio = 30 min (intervalo negativo em 100ns).
        var lockoutDuration = (-TimeSpan.FromMinutes(30).Ticks).ToString();
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("lockoutDuration", lockoutDuration));
        var locked = User("locked", Attr("userAccountControl", "512"),
            Attr("lockoutTime", FileTime(Now.AddMinutes(-5))));
        var autoUnlocked = User("released", Attr("userAccountControl", "512"),
            Attr("lockoutTime", FileTime(Now.AddMinutes(-40))));

        var result = Find(Result([domain, locked, autoUnlocked]), IndicatorCatalog.AccountLocked);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
        Assert.Equal("locked", result.Items[0].Display);
    }

    [Fact]
    public void AccountExpiring_FlagsWithinHorizon()
    {
        var expiring = User("expiring", Attr("userAccountControl", "512"),
            Attr("accountExpires", FileTime(Now.AddDays(2))));
        var never = User("perm", Attr("userAccountControl", "512"),
            Attr("accountExpires", "0"));

        var result = Find(Result([expiring, never]), IndicatorCatalog.AccountExpiring);

        Assert.NotNull(result);
        Assert.Equal(1, result!.Count);
    }

    [Fact]
    public void DisabledIndicator_IsSkipped()
    {
        var soon = User("soon", Attr("userAccountControl", "512"),
            Attr("msDS-UserPasswordExpiryTimeComputed", FileTime(Now.AddDays(3))));
        var profile = new RuleProfile("P", false, new RuleThresholds(), [])
        {
            DisabledIndicators = [IndicatorCatalog.PasswordExpiring]
        };

        Assert.Null(Find(Result([soon]), IndicatorCatalog.PasswordExpiring, profile));
    }

    [Fact]
    public void CustomIndicator_MapsMatchesWhenEnabled()
    {
        var matched = User("svc1", Attr("displayName", "Service One"));
        var match = new CustomIndicatorMatch("ci-1", "SPN users", [matched]);
        var profile = new RuleProfile("P", false, new RuleThresholds(), [])
        {
            CustomIndicators = [new CustomIndicatorDef("ci-1", "SPN users", "Ldap", "(servicePrincipalName=*)", "User", true)]
        };

        var result = Find(Result([matched], [match]), "ci-1", profile);

        Assert.NotNull(result);
        Assert.True(result!.IsCustom);
        Assert.Equal(1, result.Count);
    }

    [Fact]
    public void CustomIndicator_SkippedWhenDisabledInProfile()
    {
        var match = new CustomIndicatorMatch("ci-1", "SPN users", [User("svc1")]);
        var profile = new RuleProfile("P", false, new RuleThresholds(), [])
        {
            CustomIndicators = [new CustomIndicatorDef("ci-1", "SPN users", "Ldap", "(x=*)", "User", false)]
        };

        Assert.Null(Find(Result([], [match]), "ci-1", profile));
    }
}
