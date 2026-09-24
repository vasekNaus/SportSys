using ClosedXML.Excel;
using SportSys.Contract.Models;
using SportSys.Razor.Services;

namespace SportSys.Razor.Tests;

public class TrainingScheduleExcelExporterTests
{
    [Fact]
    public void Export_CreatesTypedWorksheetWithRequiredColumns()
    {
        var groupId = Guid.NewGuid();
        var trainings = new[]
        {
            CreateTraining(
                1,
                new DateOnly(2026, 9, 7),
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId,
                "Zimní stadion",
                "Led",
                ["Novák"]),
            CreateTraining(
                2,
                new DateOnly(2026, 9, 7),
                "U14",
                2,
                new TimeOnly(16, 30),
                new TimeOnly(18, 0),
                groupId,
                "Tělocvična",
                "Suchá",
                ["Svoboda"]),
        };

        var content = new TrainingScheduleExcelExporter().Export(trainings);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Tréninky");

        Assert.Equal(
            [
                "Kategorie",
                "Datum",
                "Čas od",
                "Čas do",
                "Typ tréninku",
                "Lokalita",
                "Trenéři",
                "Stav",
            ],
            worksheet.Row(1).Cells(1, 8).Select(cell => cell.GetString()));
        Assert.Equal("U12 + U14", worksheet.Cell(2, 1).GetString());
        Assert.Equal(new DateTime(2026, 9, 7), worksheet.Cell(2, 2).GetDateTime());
        Assert.Equal(new TimeSpan(16, 0, 0), worksheet.Cell(2, 3).GetTimeSpan());
        Assert.Equal(new TimeSpan(18, 0, 0), worksheet.Cell(2, 4).GetTimeSpan());
        Assert.Equal("Led, Suchá", worksheet.Cell(2, 5).GetString());
        Assert.Equal("Tělocvična, Zimní stadion", worksheet.Cell(2, 6).GetString());
        Assert.Equal("Novák, Svoboda", worksheet.Cell(2, 7).GetString());
        Assert.Equal(string.Empty, worksheet.Cell(2, 8).GetString());
        Assert.Equal("dd.MM.yyyy", worksheet.Cell(2, 2).Style.DateFormat.Format);
        Assert.Equal("hh:mm", worksheet.Cell(2, 3).Style.DateFormat.Format);
        Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void Export_WritesStateNameForSingleTraining()
    {
        var trainings = new[]
        {
            CreateTraining(
                1,
                new DateOnly(2026, 9, 7),
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                trainingStateId: 2,
                trainingStateName: "Potvrzený KIS"),
        };

        var content = new TrainingScheduleExcelExporter().Export(trainings);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Tréninky");

        Assert.Equal("Potvrzený KIS", worksheet.Cell(2, 8).GetString());
    }

    [Fact]
    public void Export_WritesStateNameWhenMergedTrainingsShareState()
    {
        var groupId = Guid.NewGuid();
        var trainings = new[]
        {
            CreateTraining(
                1,
                new DateOnly(2026, 9, 7),
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId,
                trainingStateId: 1,
                trainingStateName: "Plán"),
            CreateTraining(
                2,
                new DateOnly(2026, 9, 7),
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                groupId,
                trainingStateId: 1,
                trainingStateName: "Plán"),
        };

        var content = new TrainingScheduleExcelExporter().Export(trainings);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Tréninky");

        Assert.Equal("Plán", worksheet.Cell(2, 8).GetString());
    }

    [Fact]
    public void Export_LeavesStateEmptyWhenMergedTrainingsHaveDifferentStates()
    {
        var groupId = Guid.NewGuid();
        var trainings = new[]
        {
            CreateTraining(
                1,
                new DateOnly(2026, 9, 7),
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId,
                trainingStateId: 1,
                trainingStateName: "Plán"),
            CreateTraining(
                2,
                new DateOnly(2026, 9, 7),
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                groupId,
                trainingStateId: 5,
                trainingStateName: "Zrušený"),
        };

        var content = new TrainingScheduleExcelExporter().Export(trainings);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Tréninky");

        Assert.Equal(string.Empty, worksheet.Cell(2, 8).GetString());
    }

    [Fact]
    public void Export_DoesNotMergeSameGroupAcrossDifferentDates()
    {
        var groupId = Guid.NewGuid();
        var trainings = new[]
        {
            CreateTraining(
                1,
                new DateOnly(2026, 9, 7),
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId),
            CreateTraining(
                2,
                new DateOnly(2026, 9, 8),
                "U14",
                2,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                groupId),
        };

        var content = new TrainingScheduleExcelExporter().Export(trainings);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        Assert.Equal(3, workbook.Worksheet("Tréninky").LastRowUsed()!.RowNumber());
    }

    [Fact]
    public void Export_MergesVisualizationGroupWithoutPersistedGroup()
    {
        var visualizationGroupId = Guid.NewGuid();
        var trainings = new[]
        {
            CreateTraining(
                1,
                new DateOnly(2026, 9, 7),
                "U12",
                1,
                new TimeOnly(16, 0),
                new TimeOnly(17, 0),
                visualizationGroupId: visualizationGroupId),
            CreateTraining(
                2,
                new DateOnly(2026, 9, 7),
                "U14",
                2,
                new TimeOnly(17, 0),
                new TimeOnly(18, 0),
                visualizationGroupId: visualizationGroupId),
        };

        var content = new TrainingScheduleExcelExporter().Export(trainings);

        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Tréninky");

        Assert.Equal(2, worksheet.LastRowUsed()!.RowNumber());
        Assert.Equal("U12 + U14", worksheet.Cell(2, 1).GetString());
        Assert.Equal(new TimeSpan(16, 0, 0), worksheet.Cell(2, 3).GetTimeSpan());
        Assert.Equal(new TimeSpan(18, 0, 0), worksheet.Cell(2, 4).GetTimeSpan());
    }

    private static TrainingScheduleItemDto CreateTraining(
        int id,
        DateOnly date,
        string category,
        int categoryOrder,
        TimeOnly timeFrom,
        TimeOnly timeTo,
        Guid? groupId = null,
        string location = "",
        string trainingType = "Led",
        IReadOnlyList<string>? coaches = null,
        Guid? visualizationGroupId = null,
        int? trainingStateId = null,
        string? trainingStateName = null)
        => new()
        {
            Id = id,
            Date = date,
            From = date,
            To = date,
            DayName = date.DayOfWeek.ToString(),
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            GroupId = groupId,
            VisualizationGroupId = visualizationGroupId,
            SeasonCategoryName = category,
            SeasonCategoryOrder = categoryOrder,
            Location = location,
            TrainingTypeName = trainingType,
            TrainingPhaseName = "Season",
            CoachFullNames = coaches ?? [],
            TrainingStateId = trainingStateId,
            TrainingStateName = trainingStateName,
        };
}
