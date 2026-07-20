using Direnix.Core.Collection;
using Direnix.Core.Findings;
using Direnix.Core.Rules;
using Xunit;
using static Direnix.Core.Tests.TestData;

namespace Direnix.Core.Tests;

public class HygieneRulesTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static RuleContext Context(params CollectedObject[] objects) =>
        new(objects, "corp.local", new RuleThresholds(), new BusinessCriticality(), Now);

    [Fact]
    public void StaleUserRule_FlagsOnlyEnabledStaleUsers()
    {
        var stale = Make(AdObjectType.User, "CN=old,DC=corp,DC=local", "old", "S-1-5-21-1-2-3-1101",
            Attr("userAccountControl", "512"),
            Attr("lastLogonTimestamp", FileTime(Now.AddDays(-200))));
        var active = Make(AdObjectType.User, "CN=new,DC=corp,DC=local", "new", "S-1-5-21-1-2-3-1102",
            Attr("userAccountControl", "512"),
            Attr("lastLogonTimestamp", FileTime(Now.AddDays(-5))));

        var findings = new StaleUserRule().Evaluate(Context(stale, active)).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal("ADCLN-USER-STALE-001", finding.RuleId);
        Assert.Equal(Severity.Medium, finding.Severity);
        Assert.Equal("old", finding.ObjectDisplay);
    }

    [Fact]
    public void StaleUserRule_EscalatesPrivilegedMember()
    {
        var admin = Make(AdObjectType.User, "CN=adm,DC=corp,DC=local", "adm", "S-1-5-21-1-2-3-1103",
            Attr("userAccountControl", "512"),
            Attr("lastLogonTimestamp", FileTime(Now.AddDays(-200))));
        var domainAdmins = Make(AdObjectType.Group, "CN=Domain Admins,DC=corp,DC=local", "Domain Admins", "S-1-5-21-1-2-3-512",
            ("member", ["CN=adm,DC=corp,DC=local"]));

        var ctx = Context(admin, domainAdmins);
        var stale = Assert.Single(new StaleUserRule().Evaluate(ctx).ToList());
        Assert.Equal(Severity.High, stale.Severity);

        var exposure = Assert.Single(new PrivilegedGroupExposureRule().Evaluate(ctx).ToList());
        Assert.Equal("ADPRV-T0-GROUPS-001", exposure.RuleId);
        Assert.Contains("Domain Admins", exposure.Evidence["privilegedGroups"]);
    }

    [Fact]
    public void StaleUserRule_IgnoresBuiltInAdministrator()
    {
        // Administrator interno (RID 500) e habilitado e quase sempre "stale",
        // mas nao pode ser removido -> nao deve ser reportado.
        var administrator = Make(AdObjectType.User, "CN=Administrator,DC=corp,DC=local", "Administrator", "S-1-5-21-1-2-3-500",
            Attr("userAccountControl", "512"),
            Attr("lastLogonTimestamp", FileTime(Now.AddDays(-400))));

        Assert.Empty(new StaleUserRule().Evaluate(Context(administrator)));
    }

    [Fact]
    public void PrivilegedGroupExposureRule_IgnoresBuiltInAdministratorMember()
    {
        // Administrator (RID 500) e membro de Domain Admins por design.
        var administrator = Make(AdObjectType.User, "CN=Administrator,DC=corp,DC=local", "Administrator", "S-1-5-21-1-2-3-500",
            Attr("userAccountControl", "512"));
        var customAdmin = Make(AdObjectType.User, "CN=joe,DC=corp,DC=local", "joe", "S-1-5-21-1-2-3-1105",
            Attr("userAccountControl", "512"));
        var domainAdmins = Make(AdObjectType.Group, "CN=Domain Admins,DC=corp,DC=local", "Domain Admins", "S-1-5-21-1-2-3-512",
            ("member", ["CN=Administrator,DC=corp,DC=local", "CN=joe,DC=corp,DC=local"]));

        var findings = new PrivilegedGroupExposureRule().Evaluate(Context(administrator, customAdmin, domainAdmins)).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal("joe", finding.ObjectDisplay);
    }

    [Fact]
    public void UnconstrainedDelegationRule_ExcludesDomainControllers()
    {
        var server = Make(AdObjectType.Computer, "CN=APP01,DC=corp,DC=local", "APP01$", "S-1-5-21-1-2-3-1201",
            Attr("userAccountControl", "528384")); // workstation + trusted for delegation
        var dc = Make(AdObjectType.Computer, "CN=DC01,DC=corp,DC=local", "DC01$", "S-1-5-21-1-2-3-1000",
            Attr("userAccountControl", "532480")); // server trust + trusted for delegation

        var findings = new UnconstrainedDelegationRule().Evaluate(Context(server, dc)).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal("APP01$", finding.ObjectDisplay);
        Assert.Equal(Severity.Critical, finding.Severity);
    }

    [Fact]
    public void MachineAccountQuotaRule_FlagsValueAboveExpected()
    {
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("ms-DS-MachineAccountQuota", "10"));

        var finding = Assert.Single(new MachineAccountQuotaRule().Evaluate(Context(domain)).ToList());
        Assert.Equal("ADAUTH-MAQ-009", finding.RuleId);
        Assert.Equal("10", finding.Evidence["measuredValue"]);
    }

    [Fact]
    public void MachineAccountQuotaRule_SilentWhenCompliant()
    {
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("ms-DS-MachineAccountQuota", "0"));

        Assert.Empty(new MachineAccountQuotaRule().Evaluate(Context(domain)));
    }

    [Fact]
    public void KrbtgtAgeRule_FlagsOldPassword()
    {
        var krbtgt = Make(AdObjectType.User, "CN=krbtgt,DC=corp,DC=local", "krbtgt", "S-1-5-21-1-2-3-502",
            Attr("userAccountControl", "514"),
            Attr("pwdLastSet", FileTime(Now.AddDays(-400))));

        var finding = Assert.Single(new KrbtgtAgeRule().Evaluate(Context(krbtgt)).ToList());
        Assert.Equal("ADPRV-KRBTGT-AGE-010", finding.RuleId);
    }

    [Fact]
    public void RecycleBinRule_FlagsDisabled()
    {
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("recycleBinEnabled", "false"));

        var finding = Assert.Single(new RecycleBinRule().Evaluate(Context(domain)).ToList());
        Assert.Equal("ADGOV-RECYCLEBIN-001", finding.RuleId);
        Assert.Equal(Severity.High, finding.Severity);
        Assert.Equal("false", finding.Evidence["enabled"]);
    }

    [Fact]
    public void RecycleBinRule_FlagsShortRetention()
    {
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("recycleBinEnabled", "true"),
            Attr("msDS-DeletedObjectLifetime", "60")); // < 180 padrao

        var finding = Assert.Single(new RecycleBinRule().Evaluate(Context(domain)).ToList());
        Assert.Equal(Severity.Medium, finding.Severity);
        Assert.Equal("60", finding.Evidence["retentionDays"]);
    }

    [Fact]
    public void RecycleBinRule_SilentWhenEnabledAndRetained()
    {
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("recycleBinEnabled", "true"),
            Attr("msDS-DeletedObjectLifetime", "365"));

        Assert.Empty(new RecycleBinRule().Evaluate(Context(domain)));
    }

    [Fact]
    public void RecycleBinRule_SilentWhenStateUnknown()
    {
        // Coletor nao conseguiu ler a config -> sem atributo -> nao inventa achado.
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("ms-DS-MachineAccountQuota", "0"));

        Assert.Empty(new RecycleBinRule().Evaluate(Context(domain)));
    }

    private const string OldDate = "20230101000000.0Z"; // ~anos atras -> passa do limite de dias
    private static string RecentGeneralizedTime() => DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss") + ".0Z";

    [Fact]
    public void EmptyGroupRule_FlagsEmptyNonBuiltInAgedOut()
    {
        var empty = Make(AdObjectType.Group, "CN=Projeto X,DC=corp,DC=local", "ProjetoX", "S-1-5-21-1-2-3-1500",
            Attr("whenChanged", OldDate));
        var withMember = Make(AdObjectType.Group, "CN=Equipe,DC=corp,DC=local", "Equipe", "S-1-5-21-1-2-3-1501",
            ("member", ["CN=joe,DC=corp,DC=local"]));
        var builtinEmpty = Make(AdObjectType.Group, "CN=Domain Users,DC=corp,DC=local", "Domain Users", "S-1-5-21-1-2-3-513",
            Attr("whenChanged", OldDate));

        var findings = new EmptyGroupRule().Evaluate(Context(empty, withMember, builtinEmpty)).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal("ProjetoX", finding.ObjectDisplay);
    }

    [Fact]
    public void EmptyGroupRule_SilentWhenRecentlyEmptied()
    {
        var recent = Make(AdObjectType.Group, "CN=Novo,DC=corp,DC=local", "Novo", "S-1-5-21-1-2-3-1510",
            Attr("whenChanged", RecentGeneralizedTime()));

        Assert.Empty(new EmptyGroupRule().Evaluate(Context(recent)));
    }

    [Fact]
    public void EmptyOuRule_FlagsOnlyEmptyNonSystemOus()
    {
        var emptyOu = Make(AdObjectType.OrganizationalUnit, "OU=Vazia,DC=corp,DC=local", null, null,
            Attr("whenChanged", OldDate));
        var fullOu = Make(AdObjectType.OrganizationalUnit, "OU=Cheia,DC=corp,DC=local", null, null, Attr("whenChanged", OldDate));
        var child = Make(AdObjectType.User, "CN=ana,OU=Cheia,DC=corp,DC=local", "ana", "S-1-5-21-1-2-3-1600");
        var dcOu = Make(AdObjectType.OrganizationalUnit, "OU=Domain Controllers,DC=corp,DC=local", null, null, Attr("whenChanged", OldDate));

        var findings = new EmptyOuRule().Evaluate(Context(emptyOu, fullOu, child, dcOu)).ToList();

        var finding = Assert.Single(findings);
        Assert.Contains("Vazia", finding.Evidence["distinguishedName"]);
    }

    [Fact]
    public void UnlinkedGpoRule_FlagsGpoWithoutLink()
    {
        var linkedGuid = "11111111-1111-1111-1111-111111111111";
        var unlinkedGuid = "22222222-2222-2222-2222-222222222222";
        var linked = MakeGpo(linkedGuid);
        var unlinked = MakeGpo(unlinkedGuid);
        var ou = Make(AdObjectType.OrganizationalUnit, "OU=Setor,DC=corp,DC=local", null, null,
            ("gPLink", [$"[LDAP://cn={{{linkedGuid}}},cn=policies,cn=system,DC=corp,DC=local;0]"]));

        var findings = new UnlinkedGpoRule().Evaluate(Context(linked, unlinked, ou)).ToList();

        var finding = Assert.Single(findings);
        Assert.Equal(unlinkedGuid, finding.Evidence["guid"]);
    }

    private static CollectedObject OuLinking(params string[] gpoGuids)
    {
        var links = gpoGuids.Select(g => $"[LDAP://cn={{{g}}},cn=policies,cn=system,DC=corp,DC=local;0]");
        return Make(AdObjectType.OrganizationalUnit, "OU=Linker,DC=corp,DC=local", null, null,
            ("gPLink", [string.Concat(links)]));
    }

    [Fact]
    public void EmptyGpoRule_FlagsVersionZeroWhenLinked()
    {
        var emptyGuid = "33333333-3333-3333-3333-333333333333";
        var empty = MakeGpo(emptyGuid, Attr("versionNumber", "0"), Attr("displayName", "GPO Vazia"));
        var configured = MakeGpo("44444444-4444-4444-4444-444444444444", Attr("versionNumber", "5"));

        var finding = Assert.Single(new EmptyGpoRule().Evaluate(Context(empty, configured, OuLinking(emptyGuid))).ToList());
        Assert.Equal("GPO Vazia", finding.ObjectDisplay);
    }

    [Fact]
    public void EmptyGpoRule_SilentWhenUnlinked()
    {
        var empty = MakeGpo("33333333-3333-3333-3333-333333333333", Attr("versionNumber", "0"));
        // Sem link: cabe a regra de GPO nao vinculada, nao a de GPO vazia (sem duplicar).
        Assert.Empty(new EmptyGpoRule().Evaluate(Context(empty)));
    }

    [Fact]
    public void DisabledGpoRule_FlagsBothSectionsDisabledWhenLinked()
    {
        var disabledGuid = "55555555-5555-5555-5555-555555555555";
        var disabled = MakeGpo(disabledGuid, Attr("flags", "3"));
        var active = MakeGpo("66666666-6666-6666-6666-666666666666", Attr("flags", "0"));

        Assert.Single(new DisabledGpoRule().Evaluate(Context(disabled, active, OuLinking(disabledGuid))).ToList());
    }

    [Fact]
    public void PasswordNotRequiredRule_Flags()
    {
        var user = Make(AdObjectType.User, "CN=svc,DC=corp,DC=local", "svc", "S-1-5-21-1-2-3-1700",
            Attr("userAccountControl", "544")); // 512 + PASSWD_NOTREQD
        Assert.Single(new PasswordNotRequiredRule().Evaluate(Context(user)).ToList());
    }

    [Fact]
    public void PasswordNeverExpiresRule_FlagsEnabled()
    {
        var user = Make(AdObjectType.User, "CN=ned,DC=corp,DC=local", "ned", "S-1-5-21-1-2-3-1701",
            Attr("userAccountControl", "66048")); // 512 + DONT_EXPIRE_PASSWORD
        Assert.Single(new PasswordNeverExpiresRule().Evaluate(Context(user)).ToList());
    }

    [Fact]
    public void PasswordNeverExpiresRule_SkipsManagedAccounts()
    {
        // gMSA / conta gerenciada (sAMAccountName terminando em '$'): AD rotaciona a senha.
        var gmsa = Make(AdObjectType.User, "CN=svc-app,DC=corp,DC=local", "svc-app$", "S-1-5-21-1-2-3-1710",
            Attr("userAccountControl", "66048"));
        Assert.Empty(new PasswordNeverExpiresRule().Evaluate(Context(gmsa)));
    }

    [Fact]
    public void ReversiblePasswordRule_Flags()
    {
        var user = Make(AdObjectType.User, "CN=rev,DC=corp,DC=local", "rev", "S-1-5-21-1-2-3-1702",
            Attr("userAccountControl", "640")); // 512 + ENCRYPTED_TEXT_PWD_ALLOWED
        Assert.Single(new ReversiblePasswordRule().Evaluate(Context(user)).ToList());
    }

    [Fact]
    public void KerberoastRule_FlagsUserWithSpnAndEscalatesPrivileged()
    {
        var svc = Make(AdObjectType.User, "CN=sql,DC=corp,DC=local", "sql", "S-1-5-21-1-2-3-1703",
            Attr("userAccountControl", "512"), ("servicePrincipalName", ["MSSQLSvc/db01"]));
        var f1 = Assert.Single(new KerberoastRule().Evaluate(Context(svc)).ToList());
        Assert.Equal(Severity.High, f1.Severity);

        var privSvc = Make(AdObjectType.User, "CN=adm,DC=corp,DC=local", "adm", "S-1-5-21-1-2-3-1704",
            Attr("userAccountControl", "512"), ("servicePrincipalName", ["HTTP/app"]));
        var da = Make(AdObjectType.Group, "CN=Domain Admins,DC=corp,DC=local", "Domain Admins", "S-1-5-21-1-2-3-512",
            ("member", ["CN=adm,DC=corp,DC=local"]));
        var f2 = new KerberoastRule().Evaluate(Context(privSvc, da)).Single();
        Assert.Equal(Severity.Critical, f2.Severity);
    }

    [Fact]
    public void LegacyOsComputerRule_FlagsUnsupportedOs()
    {
        var old = Make(AdObjectType.Computer, "CN=SRV03,DC=corp,DC=local", "SRV03$", "S-1-5-21-1-2-3-1800",
            Attr("userAccountControl", "4096"), Attr("operatingSystem", "Windows Server 2008 R2 Standard"));
        var modern = Make(AdObjectType.Computer, "CN=SRV22,DC=corp,DC=local", "SRV22$", "S-1-5-21-1-2-3-1801",
            Attr("userAccountControl", "4096"), Attr("operatingSystem", "Windows Server 2022 Datacenter"));

        var finding = Assert.Single(new LegacyOsComputerRule().Evaluate(Context(old, modern)).ToList());
        Assert.Equal("SRV03$", finding.ObjectDisplay);
    }

    [Fact]
    public void OrphanAdminCountRule_FlagsResidualAdminCount()
    {
        var orphan = Make(AdObjectType.User, "CN=exadmin,DC=corp,DC=local", "exadmin", "S-1-5-21-1-2-3-1900",
            Attr("adminCount", "1"));
        Assert.Single(new OrphanAdminCountRule().Evaluate(Context(orphan)).ToList());
    }

    [Fact]
    public void OrphanAdminCountRule_SilentWhenStillPrivileged()
    {
        var member = Make(AdObjectType.User, "CN=curadmin,DC=corp,DC=local", "curadmin", "S-1-5-21-1-2-3-1901",
            Attr("adminCount", "1"));
        var da = Make(AdObjectType.Group, "CN=Domain Admins,DC=corp,DC=local", "Domain Admins", "S-1-5-21-1-2-3-512",
            ("member", ["CN=curadmin,DC=corp,DC=local"]));
        Assert.Empty(new OrphanAdminCountRule().Evaluate(Context(member, da)));
    }

    [Fact]
    public void ConflictAndLostFoundRules_ReadDomainSignals()
    {
        var domain = Make(AdObjectType.Domain, "DC=corp,DC=local", null, "S-1-5-21-1-2-3",
            Attr("conflictObjectCount", "2"), Attr("lostFoundCount", "1"));

        Assert.Single(new ConflictObjectsRule().Evaluate(Context(domain)).ToList());
        Assert.Single(new LostAndFoundRule().Evaluate(Context(domain)).ToList());
    }

    [Fact]
    public void KerberosPreauthRule_FlagsDisabledPreauth()
    {
        var user = Make(AdObjectType.User, "CN=asrep,DC=corp,DC=local", "asrep", "S-1-5-21-1-2-3-1301",
            Attr("userAccountControl", "4194816")); // 512 + DONT_REQ_PREAUTH

        var finding = Assert.Single(new KerberosPreauthRule().Evaluate(Context(user)).ToList());
        Assert.Equal("ADAUTH-KRB-PREAUTH-006", finding.RuleId);
        Assert.Equal(Severity.High, finding.Severity);
    }
}
