using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Tests;

public class WeekDayNamesTests
{
    private sealed record Item(int Id, DayOfWeek Day);

    private static readonly IReadOnlyList<Item> Items =
    [
        new Item(1, DayOfWeek.Monday),
        new Item(2, DayOfWeek.Tuesday),
        new Item(3, DayOfWeek.Wednesday),
        new Item(4, DayOfWeek.Sunday),
    ];

    [Fact]
    public void OrderedDays_StartsOnMondayAndEndsOnSunday()
    {
        Assert.Equal(
            [
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday,
                DayOfWeek.Sunday,
            ],
            WeekDayNames.OrderedDays);
    }

    [Theory]
    [InlineData(DayOfWeek.Monday, "Pondělí")]
    [InlineData(DayOfWeek.Tuesday, "Úterý")]
    [InlineData(DayOfWeek.Wednesday, "Středa")]
    [InlineData(DayOfWeek.Thursday, "Čtvrtek")]
    [InlineData(DayOfWeek.Friday, "Pátek")]
    [InlineData(DayOfWeek.Saturday, "Sobota")]
    [InlineData(DayOfWeek.Sunday, "Neděle")]
    public void GetFullName_ReturnsCzechName(DayOfWeek day, string expected)
    {
        Assert.Equal(expected, WeekDayNames.GetFullName(day));
    }

    [Fact]
    public void FilterByDay_WithEmptySelection_ReturnsAllItemsInOriginalOrder()
    {
        var result = WeekDayNames.FilterByDay(Items, item => item.Day, []);

        Assert.Equal(Items.Select(item => item.Id), result.Select(item => item.Id));
    }

    [Fact]
    public void FilterByDay_WithSelection_ReturnsOnlyMatchingItems()
    {
        var result = WeekDayNames.FilterByDay(
            Items,
            item => item.Day,
            [DayOfWeek.Monday, DayOfWeek.Sunday]);

        Assert.Equal([1, 4], result.Select(item => item.Id));
    }

    [Fact]
    public void FilterByDay_WithSelectionMatchingNoItems_ReturnsEmptyList()
    {
        var result = WeekDayNames.FilterByDay(
            Items,
            item => item.Day,
            [DayOfWeek.Saturday]);

        Assert.Empty(result);
    }
}
