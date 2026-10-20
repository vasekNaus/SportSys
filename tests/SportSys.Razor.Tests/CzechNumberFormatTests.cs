using SportSys.Razor.Infrastructure;

namespace SportSys.Razor.Tests;

public class CzechNumberFormatTests
{
    [Fact]
    public void FormatDecimal_UsesCzechCultureAndPreservesDecimalScale()
    {
        Assert.Equal("1,5", CzechNumberFormat.FormatDecimal(1.5m));
        Assert.Equal("1,50", CzechNumberFormat.FormatDecimal(1.50m));
        Assert.Equal("12500", CzechNumberFormat.FormatDecimal(12500m));
    }

    [Fact]
    public void FormatDecimal_NullReturnsEmptyString()
        => Assert.Equal(string.Empty, CzechNumberFormat.FormatDecimal(null));

    [Fact]
    public void FormatDecimal_AppliesCompositeFormat()
        => Assert.Equal("1,50", CzechNumberFormat.FormatDecimal(1.5m, "{0:0.00}"));

    [Theory]
    [InlineData("1,5", "1.5")]
    [InlineData("1.5", "1.5")]
    [InlineData(" 1,50 ", "1.5")]
    [InlineData("-2,25", "-2.25")]
    [InlineData("1 234,5", "1234.5")]
    [InlineData("1\u00A0234,5", "1234.5")]
    [InlineData("1\u202F234,5", "1234.5")]
    [InlineData(",5", "0.5")]
    public void TryParseDecimal_AcceptsCzechAndInvariantDecimalSeparators(
        string value,
        string expected)
    {
        Assert.True(CzechNumberFormat.TryParseDecimal(value, out var result));
        Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), result);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("1,2,3")]
    [InlineData("1.234,5")]
    [InlineData("1,.")]
    [InlineData("1,")]
    [InlineData(".")]
    public void TryParseDecimal_RejectsInvalidOrAmbiguousValues(string value)
        => Assert.False(CzechNumberFormat.TryParseDecimal(value, out _));
}
