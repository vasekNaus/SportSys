using ClosedXML.Excel;
using SportSys.Contract.Models;
using SportSys.Razor.Models;
using SportSys.Razor.Services;

namespace SportSys.Razor.Tests;

public class TrainingOverviewExcelExporterTests
{
    private readonly TrainingOverviewExcelExporter _exporter = new();

    [Fact]
    public void ExportRequirements_WritesDisplayColumnsAndTypedValues()
    {
        var requirement = new TrainingRequirementListItem
        {
            SeasonName = "2026/2027",
            SeasonCategoryCode = "U12",
            SeasonCategoryName = "Mladší žáci",
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2027, 3, 31),
            DurationHours = 2.5m,
            CoachAssignments =
            [
                new TrainingRequirementCoachListItem
                {
                    Id = 1,
                    DisplayName = "Jan Novák",
                    CoachRoleCode = "H",
                    CoachRoleId = 1,
                    CoachRoleName = "Hlavní",
                    CoachRoleOrder = 1,
                    Hours = 1.5m,
                },
                new TrainingRequirementCoachListItem
                {
                    Id = 2,
                    DisplayName = "Petr Zíma",
                    CoachRoleCode = "F",
                    CoachRoleId = 2,
                    CoachRoleName = "Fyzioterapeut",
                    CoachRoleOrder = 2,
                    Hours = 0.75m,
                },
            ],
        };

        using var workbook = Open(_exporter.ExportRequirements([requirement]));
        var worksheet = workbook.Worksheet("Požadavky");

        AssertHeaders(
            worksheet,
            [
                "Sezóna", "Kategorie", "Typ tréninku", "Fáze", "Od", "Do",
                "Rozsah [h]", "Hlavní", "Asistent", "Brankáři", "Obránci", "Trenéři",
            ]);
        Assert.Equal("2026/2027", worksheet.Cell(2, 1).GetString());
        Assert.Equal(new DateTime(2026, 9, 1), worksheet.Cell(2, 5).GetDateTime());
        Assert.Equal(new DateTime(2027, 3, 31), worksheet.Cell(2, 6).GetDateTime());
        Assert.Equal(2.5m, worksheet.Cell(2, 7).GetValue<decimal>());
        Assert.Equal("Novák (1,5)", worksheet.Cell(2, 8).GetString());
        Assert.Equal("-", worksheet.Cell(2, 9).GetString());
        Assert.Equal("Fyzioterapeut: Zíma (0,75)", worksheet.Cell(2, 12).GetString());
        Assert.Equal("dd.MM.yyyy", worksheet.Cell(2, 5).Style.DateFormat.Format);
        AssertCommonFormatting(worksheet);
    }

    [Fact]
    public void ExportPlans_UsesVisibleColumnsWithoutActionAndKeepsValuesTyped()
    {
        var plan = new TrainingPlanScheduleItemDto
        {
            SeasonCategoryCode = "U12",
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
            LocationName = "Zimní stadion",
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2027, 3, 31),
            Title = "Ranní trénink",
            DayName = nameof(DayOfWeek.Monday),
            TimeFrom = new TimeOnly(16, 0),
            TimeTo = new TimeOnly(17, 30),
            DurationMinutes = 90,
            CoachAssignments =
            [
                new TrainingPlanCoachDto
                {
                    CoachId = 1,
                    FullName = "Jan Novák",
                    LastName = "Novák",
                    CoachRoleId = 1,
                    CoachRoleCode = "H",
                    CoachRoleOrder = 1,
                    CoachRoleName = "Hlavní",
                },
            ],
        };

        using var workbook = Open(_exporter.ExportPlans([plan], "2026/2027"));
        var worksheet = workbook.Worksheet("Plány");

        AssertHeaders(
            worksheet,
            [
                "Sezóna", "Kategorie", "Typ tréninku", "Fáze", "Lokalita", "Od",
                "Do", "Název", "Den v týdnu", "Čas od", "Čas do", "Doba trvání", "Trenéři",
            ]);
        Assert.Equal("2026/2027", worksheet.Cell(2, 1).GetString());
        Assert.Equal(new DateTime(2026, 9, 1), worksheet.Cell(2, 6).GetDateTime());
        Assert.Equal(new DateTime(2027, 3, 31), worksheet.Cell(2, 7).GetDateTime());
        Assert.Equal(new TimeSpan(16, 0, 0), worksheet.Cell(2, 10).GetTimeSpan());
        Assert.Equal(new TimeSpan(17, 30, 0), worksheet.Cell(2, 11).GetTimeSpan());
        Assert.Equal(90, worksheet.Cell(2, 12).GetValue<int>());
        Assert.Equal("Novák (H)", worksheet.Cell(2, 13).GetString());
        Assert.Equal("dd.MM.yyyy", worksheet.Cell(2, 6).Style.DateFormat.Format);
        Assert.Equal("hh:mm", worksheet.Cell(2, 10).Style.DateFormat.Format);
        AssertCommonFormatting(worksheet);
    }

    [Fact]
    public void ExportCoverage_WritesNumericHoursAndStatus()
    {
        var coverage = new TrainingRequirementCoverageRow
        {
            SeasonName = "2026/2027",
            SeasonCategoryCode = "U12",
            SeasonCategoryName = "Mladší žáci",
            TrainingTypeName = "Led",
            CoachName = "Jan Novák",
            CoachRoleName = "Hlavní",
            RequiredHours = 2.5m,
            PlannedHours = 2m,
            Status = TrainingRequirementCoverageStatus.UnderPlan,
        };

        using var workbook = Open(_exporter.ExportCoverage([coverage]));
        var worksheet = workbook.Worksheet("Pokrytí");

        AssertHeaders(
            worksheet,
            [
                "Sezóna", "Kategorie", "Typ tréninku", "Trenér", "Role",
                "Požadavek [h]", "Plán [h]", "Status",
            ]);
        Assert.Equal(2.5m, worksheet.Cell(2, 6).GetValue<decimal>());
        Assert.Equal(2m, worksheet.Cell(2, 7).GetValue<decimal>());
        Assert.Equal("Nesplňuje plán", worksheet.Cell(2, 8).GetString());
        AssertCommonFormatting(worksheet);
    }

    [Fact]
    public void ExportRequirements_RejectsEmptyRows()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => _exporter.ExportRequirements([]));
    }

    private static XLWorkbook Open(byte[] content)
        => new(new MemoryStream(content));

    private static void AssertHeaders(IXLWorksheet worksheet, string[] expected)
        => Assert.Equal(
            expected,
            worksheet.Row(1).Cells(1, expected.Length).Select(cell => cell.GetString()));

    private static void AssertCommonFormatting(IXLWorksheet worksheet)
    {
        Assert.True(worksheet.Cell(1, 1).Style.Font.Bold);
        Assert.True(worksheet.AutoFilter.IsEnabled);
        Assert.Equal(1, worksheet.SheetView.SplitRow);
        Assert.True(worksheet.Column(1).Width > 0);
    }
}
