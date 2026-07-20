using Direnix.Core.Collection;
using Xunit;

namespace Direnix.Core.Tests;

public class AdAttributesTests
{
    [Theory]
    [InlineData("512", true)]   // NormalAccount habilitado
    [InlineData("514", false)]  // NormalAccount + AccountDisabled
    [InlineData("66048", true)] // NormalAccount + DontExpirePassword
    public void IsEnabled_ReadsDisabledFlag(string uac, bool expected) =>
        Assert.Equal(expected, AdAttributes.IsEnabled(uac));

    [Fact]
    public void ParseUac_DetectsTrustedForDelegation() =>
        Assert.True(AdAttributes.HasFlag("524800", UserAccountControlFlags.TrustedForDelegation));

    [Fact]
    public void ParseFileTime_RoundTripsWithinTolerance()
    {
        var when = DateTimeOffset.UtcNow.AddDays(-30);
        var parsed = AdAttributes.ParseFileTime(when.ToFileTime().ToString());

        Assert.NotNull(parsed);
        Assert.True(Math.Abs((parsed!.Value - when).TotalSeconds) < 1);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("9223372036854775807")] // long.MaxValue = "nunca expira"
    [InlineData("texto")]
    public void ParseFileTime_ReturnsNullForSentinels(string raw) =>
        Assert.Null(AdAttributes.ParseFileTime(raw));

    [Fact]
    public void ParseGeneralizedTime_ParsesAdFormat()
    {
        var parsed = AdAttributes.ParseGeneralizedTime("20240115093000.0Z");

        Assert.NotNull(parsed);
        Assert.Equal(new DateTime(2024, 1, 15, 9, 30, 0, DateTimeKind.Utc), parsed!.Value.UtcDateTime);
    }

    [Theory]
    [InlineData("S-1-5-21-1111-2222-3333-512", 512)]
    [InlineData("S-1-5-32-544", 544)]
    [InlineData("", null)]
    public void RelativeId_ExtractsRid(string sid, int? expected) =>
        Assert.Equal(expected, AdAttributes.RelativeId(sid));
}
