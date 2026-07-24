using Direnix.Core.Auth;
using Xunit;

namespace Direnix.Core.Tests;

public class PasswordPolicyTests
{
    [Theory]
    [InlineData("Direnix!2026")]   // upper+lower+digit+symbol
    [InlineData("Abcdef1234")]     // upper+lower+digit (3 classes)
    [InlineData("senha_forte9X")]  // lower+digit+symbol+upper
    public void Accepts_StrongPasswords(string pwd) => Assert.Null(PasswordPolicy.Validate(pwd));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("curta1!")]        // < 10
    [InlineData("Ab1!")]           // muito curta
    public void Rejects_TooShort(string? pwd)
    {
        var err = PasswordPolicy.Validate(pwd);
        Assert.NotNull(err);
        Assert.Contains("10", err);
    }

    [Theory]
    [InlineData("todasminusculas")]  // 1 classe
    [InlineData("1234567890")]       // 1 classe (dígitos)
    [InlineData("lowercaseonly")]    // 1 classe
    public void Rejects_TooFewClasses(string pwd)
    {
        var err = PasswordPolicy.Validate(pwd);
        Assert.NotNull(err);
    }

    [Fact]
    public void LockoutConstants_AreSane()
    {
        Assert.True(PasswordPolicy.MaxFailedAttempts >= 3);
        Assert.True(PasswordPolicy.LockoutDuration >= TimeSpan.FromMinutes(1));
    }
}
