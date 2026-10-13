using ClosedXML.Excel;
using SportSys.Contract.Services;
using SportSys.Database.Enums;
using DbTraining = SportSys.Database.Models.sport.Training;

namespace SportSys.Razor.Tests;

public class KisTrainingImportModelTests
{
    [Theory]
    [InlineData("Led", ETrainingType.Ice)]
    [InlineData("Suchá příprava", ETrainingType.Dry)]
    public void KnownTrainingTypeMapsToCodebookValue(
        string sourceType,
        ETrainingType expectedType)
    {
        var mapped = KisTrainingImportService.TryMapTrainingType(sourceType, out var trainingType);

        Assert.True(mapped);
        Assert.Equal(expectedType, trainingType);
    }

    [Fact]
    public void UnknownTrainingTypeIsRejected()
    {
        var mapped = KisTrainingImportService.TryMapTrainingType("Schůzka s rodiči", out _);

        Assert.False(mapped);
    }

    [Theory]
    [InlineData("24.10.2026", true)]
    [InlineData("2026-10-24", true)]
    [InlineData("24/10/2026", false)]
    public void DateMustUseSupportedFormat(string value, bool expectedValid)
    {
        var valid = KisTrainingImportService.TryParseDate(value, out _);

        Assert.Equal(expectedValid, valid);
    }

    [Theory]
    [InlineData("9:00", true)]
    [InlineData("09:00", true)]
    [InlineData("9.00", false)]
    public void TimeMustUseSupportedFormat(string value, bool expectedValid)
    {
        var valid = KisTrainingImportService.TryParseTime(value, out _);

        Assert.Equal(expectedValid, valid);
    }

    [Fact]
    public void IntervalMustEndAfterItStarts()
    {
        Assert.True(KisTrainingImportService.HasValidInterval(new TimeOnly(9, 0), new TimeOnly(10, 0)));
        Assert.False(KisTrainingImportService.HasValidInterval(new TimeOnly(9, 0), new TimeOnly(9, 0)));
        Assert.False(KisTrainingImportService.HasValidInterval(new TimeOnly(10, 0), new TimeOnly(9, 0)));
    }

    [Fact]
    public void RepeatedHeaderRowIsIgnoredWhenReadingWorkbook()
    {
        using var stream = new MemoryStream();
        using (var workbook = new XLWorkbook())
        {
            var worksheet = workbook.AddWorksheet("Export");
            var headers = new[]
            {
                "Datum", "Čas od", "Čas do", "Soupiska",
                "Název", "Místo", "Typ", "Poznámka trenéra",
            };
            for (var column = 0; column < headers.Length; column++)
            {
                worksheet.Cell(1, column + 1).Value = headers[column];
                worksheet.Cell(3, column + 1).Value = headers[column];
            }

            worksheet.Cell(2, 1).Value = new DateTime(2026, 10, 24);
            worksheet.Cell(2, 1).Style.DateFormat.Format = "dd.MM.yyyy";
            worksheet.Cell(2, 2).Value = "09:00";
            worksheet.Cell(2, 3).Value = "10:00";
            worksheet.Cell(2, 4).Value = "U12";
            worksheet.Cell(2, 6).Value = "Zimní stadion";
            worksheet.Cell(2, 7).Value = "Led";
            workbook.SaveAs(stream);
        }

        stream.Position = 0;
        var rows = KisTrainingImportService.ReadRows(stream);

        var row = Assert.Single(rows);
        Assert.Equal(2, row.RowNumber);
        Assert.Equal("U12", row.Squad);
    }

    [Fact]
    public void ExactIntervalConfirmsExistingTraining()
    {
        var candidate = CreateTraining(new TimeOnly(9, 0), new TimeOnly(10, 0));

        var action = KisTrainingImportService.ClassifyCandidates(
            [candidate],
            new TimeOnly(9, 0),
            new TimeOnly(10, 0));

        Assert.Equal(KisTrainingImportAction.Confirm, action);
    }

    [Fact]
    public void DifferentIntervalChangesOnlyCandidate()
    {
        var candidate = CreateTraining(new TimeOnly(9, 0), new TimeOnly(10, 0));

        var action = KisTrainingImportService.ClassifyCandidates(
            [candidate],
            new TimeOnly(10, 0),
            new TimeOnly(11, 0));

        Assert.Equal(KisTrainingImportAction.ChangeTime, action);
    }

    [Fact]
    public void NoCandidateCreatesKisOnlyTraining()
    {
        var action = KisTrainingImportService.ClassifyCandidates(
            [],
            new TimeOnly(9, 0),
            new TimeOnly(10, 0));

        Assert.Equal(KisTrainingImportAction.Insert, action);
    }

    [Fact]
    public void MultipleCandidatesAreAmbiguous()
    {
        var action = KisTrainingImportService.ClassifyCandidates(
            [CreateTraining(new TimeOnly(9, 0), new TimeOnly(10, 0)), CreateTraining(new TimeOnly(11, 0), new TimeOnly(12, 0))],
            new TimeOnly(10, 0),
            new TimeOnly(11, 0));

        Assert.Equal(KisTrainingImportAction.Ambiguous, action);
    }

    [Fact]
    public void ExactIntervalDoesNotBypassMultipleCandidates()
    {
        var action = KisTrainingImportService.ClassifyCandidates(
            [CreateTraining(new TimeOnly(9, 0), new TimeOnly(10, 0)), CreateTraining(new TimeOnly(11, 0), new TimeOnly(12, 0))],
            new TimeOnly(9, 0),
            new TimeOnly(10, 0));

        Assert.Equal(KisTrainingImportAction.Ambiguous, action);
    }

    private static DbTraining CreateTraining(TimeOnly timeFrom, TimeOnly timeTo)
        => new()
        {
            SeasonCategoryCode = "U12",
            Note = string.Empty,
            TimeFrom = timeFrom,
            TimeTo = timeTo,
        };
}
