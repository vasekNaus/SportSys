using SportSys.Razor.Infrastructure;
using System.Globalization;

namespace SportSys.Razor.Tests;

public class CzechDateTimeFormatTests
{
    [Fact]
    public void FormatDate_FormatsDateOnlyAndDateTimeInCzechStyle()
    {
        Assert.Equal("5. 10. 2026", CzechDateTimeFormat.FormatDate(new DateOnly(2026, 10, 5)));
        Assert.Equal(
            "5. 10. 2026",
            CzechDateTimeFormat.FormatDate(new DateTime(2026, 10, 5, 12, 30, 0)));
    }

    [Fact]
    public void FormatTime_FormatsTimeOnlyAndTimeSpanInTwentyFourHourStyle()
    {
        Assert.Equal("07:05", CzechDateTimeFormat.FormatTime(new TimeOnly(7, 5)));
        Assert.Equal("07:05", CzechDateTimeFormat.FormatTime(new TimeSpan(7, 5, 0)));
    }

    [Fact]
    public void Format_ReturnsEmptyStringForNullOrUnsupportedValues()
    {
        Assert.Equal(string.Empty, CzechDateTimeFormat.FormatDate(null));
        Assert.Equal(string.Empty, CzechDateTimeFormat.FormatTime(null));
        Assert.Equal(string.Empty, CzechDateTimeFormat.FormatDate("2026-10-05"));
        Assert.Equal(string.Empty, CzechDateTimeFormat.FormatTime("07:05"));
    }

    [Theory]
    [InlineData("5. 10. 2026")]
    [InlineData("5.10.2026")]
    [InlineData("05.10.2026")]
    [InlineData("2026-10-05")]
    public void TryParseDate_AcceptsCzechVariantsAndIsoFallback(string value)
    {
        Assert.True(CzechDateTimeFormat.TryParseDate(value, out var result));
        Assert.Equal(new DateOnly(2026, 10, 5), result);
    }

    [Theory]
    [InlineData("7:05")]
    [InlineData("07:05")]
    [InlineData("07:05:00")]
    public void TryParseTime_AcceptsTwentyFourHourVariants(string value)
    {
        Assert.True(CzechDateTimeFormat.TryParseTime(value, out var result));
        Assert.Equal(new TimeOnly(7, 5), result);
    }

    [Theory]
    [InlineData("10/05/2026")]
    [InlineData("31. 2. 2026")]
    [InlineData("25:00")]
    [InlineData("abc")]
    public void TryParse_RejectsInvalidValues(string value)
    {
        Assert.False(CzechDateTimeFormat.TryParseDate(value, out _));
        Assert.False(CzechDateTimeFormat.TryParseTime(value, out _));
    }

    [Fact]
    public void TryParse_IsIndependentOfCurrentCulture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");

            Assert.True(CzechDateTimeFormat.TryParseDate("5. 10. 2026", out var date));
            Assert.Equal(new DateOnly(2026, 10, 5), date);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
