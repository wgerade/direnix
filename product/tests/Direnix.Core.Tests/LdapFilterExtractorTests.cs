using Direnix.Core.Indicators;
using Xunit;

namespace Direnix.Core.Tests;

public class LdapFilterExtractorTests
{
    [Fact]
    public void Ldap_PassesBalancedFilter()
    {
        Assert.True(LdapFilterExtractor.TryBuild("Ldap", "(objectClass=user)", out var filter, out _));
        Assert.Equal("(objectClass=user)", filter);
    }

    [Fact]
    public void Ldap_WrapsBareEquality()
    {
        Assert.True(LdapFilterExtractor.TryBuild("Ldap", "sAMAccountName=jdoe", out var filter, out _));
        Assert.Equal("(sAMAccountName=jdoe)", filter);
    }

    [Fact]
    public void Ldap_RejectsUnbalancedParens()
    {
        Assert.False(LdapFilterExtractor.TryBuild("Ldap", "(objectClass=user", out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void PowerShell_ExtractsLdapFilterVerbatim()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell",
            "Get-ADUser -LDAPFilter \"(servicePrincipalName=*)\"", out var filter, out _));
        Assert.Equal("(servicePrincipalName=*)", filter);
    }

    [Fact]
    public void PowerShell_TranslatesEnabledFalse()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell",
            "Get-ADUser -Filter 'Enabled -eq $false'", out var filter, out _));
        Assert.Equal("(&(objectCategory=person)(objectClass=user)(userAccountControl:1.2.840.113556.1.4.803:=2))", filter);
    }

    [Fact]
    public void PowerShell_TranslatesLike()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell",
            "Get-ADUser -Filter 'name -like \"svc*\"'", out var filter, out _));
        Assert.Equal("(&(objectCategory=person)(objectClass=user)(name=svc*))", filter);
    }

    [Fact]
    public void PowerShell_TranslatesAndCombination()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell",
            "Get-ADUser -Filter 'Enabled -eq $true -and department -eq \"IT\"'", out var filter, out _));
        Assert.Equal("(&(objectCategory=person)(objectClass=user)(&(!(userAccountControl:1.2.840.113556.1.4.803:=2))(department=IT)))", filter);
    }

    [Fact]
    public void PowerShell_RejectsMixedAndOr()
    {
        Assert.False(LdapFilterExtractor.TryBuild("PowerShell",
            "Get-ADUser -Filter 'a -eq 1 -and b -eq 2 -or c -eq 3'", out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void PowerShell_Identity_BuildsScopedLookup()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell",
            "Get-ADComputer -Identity WIN-F93EKV954GR", out var filter, out var error));
        Assert.Null(error);
        Assert.StartsWith("(&(objectCategory=computer)(|", filter);
        Assert.Contains("(sAMAccountName=WIN-F93EKV954GR$)", filter);   // tenta a conta de maquina
        Assert.Contains("(dNSHostName=WIN-F93EKV954GR)", filter);
    }

    [Fact]
    public void PowerShell_Identity_User_ScopesToUser()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell", "Get-ADUser -Identity jdoe", out var filter, out _));
        Assert.Contains("(objectCategory=person)", filter);
        Assert.Contains("(objectClass=user)", filter);
        Assert.Contains("(sAMAccountName=jdoe)", filter);
    }

    [Fact]
    public void PowerShell_BareCmdlet_ReturnsAllOfType()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell", "Get-ADComputer", out var filter, out _));
        Assert.Equal("(objectCategory=computer)", filter);
    }

    [Fact]
    public void PowerShell_FilterCombinesWithCmdletCategory()
    {
        Assert.True(LdapFilterExtractor.TryBuild("PowerShell",
            "Get-ADComputer -Filter 'Enabled -eq $false'", out var filter, out _));
        Assert.Equal("(&(objectCategory=computer)(userAccountControl:1.2.840.113556.1.4.803:=2))", filter);
    }

    [Fact]
    public void PowerShell_ErrorsWhenUnrecognized()
    {
        Assert.False(LdapFilterExtractor.TryBuild("PowerShell", "Import-Module ActiveDirectory", out _, out var error));
        Assert.NotNull(error);
    }

    [Fact]
    public void EmptyQuery_Fails()
    {
        Assert.False(LdapFilterExtractor.TryBuild("Ldap", "  ", out _, out var error));
        Assert.NotNull(error);
    }
}
