using System.ComponentModel.DataAnnotations;
using ClosedXML.Excel;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Database.Enums;

namespace SportSys.Razor.Tests;

public class KisMatchImportModelTests
{
    [Theory]
    [InlineData("Ligový zápas", EMatchType.League)]
    [InlineData("Příprava", EMatchType.Friendly)]
    [InlineData("Turnaj", EMatchType.Tournament)]
    public void KnownMatchTypeMapsToCodebookValue(
        string sourceType,
        EMatchType expectedType)
    {
        var mapped = KisMatchImportService.TryMapMatchType(sourceType, out var matchType);

        Assert.True(mapped);
        Assert.Equal(expectedType, matchType);
    }

    [Fact]
    public void UnknownMatchTypeIsRejected()
    {
        var mapped = KisMatchImportService.TryMapMatchType("Exhibice", out _);

        Assert.False(mapped);
    }

    [Fact]
    public void MatchDurationDeterminesEndTimeWithoutWarmup()
    {
        var timeTo = KisMatchImportService.ResolveTimeTo(new TimeOnly(17, 30), 90);

        Assert.Equal(new TimeOnly(19, 0), timeTo);
    }

    [Fact]
    public void MatchIdentityRequiresCategoryAndBothTeams()
    {
        var expected = new KisMatchIdentity(
            LocationId: 1,
            Date: new DateOnly(2026, 10, 24),
            TimeFrom: new TimeOnly(9, 0),
            SeasonCategoryCode: "U12",
            HomeTeamId: 10,
            AwayTeamId: 20);
        var differentCategory = expected with { SeasonCategoryCode = "U13" };
        var differentAwayTeam = expected with { AwayTeamId = 21 };

        Assert.True(KisMatchImportService.IsEquivalent(expected, expected));
        Assert.False(KisMatchImportService.IsEquivalent(expected, differentCategory));
        Assert.False(KisMatchImportService.IsEquivalent(expected, differentAwayTeam));
    }

    [Fact]
    public void RepeatedHeaderRowIsIgnoredWhenReadingWorkbook()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.xlsx");

        try
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.AddWorksheet("Export");
                var headers = new[] { "Datum", "Čas", "Soupiska", "Domácí", "Hosté", "Místo", "Typ" };
                for (var column = 0; column < headers.Length; column++)
                {
                    worksheet.Cell(1, column + 1).Value = headers[column];
                    worksheet.Cell(3, column + 1).Value = headers[column];
                }

                worksheet.Cell(2, 1).Value = new DateTime(2026, 10, 24);
                worksheet.Cell(2, 1).Style.DateFormat.Format = "dd.MM.yyyy";
                worksheet.Cell(2, 2).Value = "09:00";
                worksheet.Cell(2, 3).Value = "U12";
                worksheet.Cell(2, 4).Value = "HC Klatovy";
                worksheet.Cell(2, 5).Value = "HC Tachov";
                worksheet.Cell(2, 6).Value = "Klatovy, Zimní stadion města Klatov";
                worksheet.Cell(2, 7).Value = "Ligový zápas";
                workbook.SaveAs(filePath);
            }

            var rows = KisMatchImportService.ReadRows(filePath);
            using var stream = File.OpenRead(filePath);
            var uploadedRows = KisMatchImportService.ReadRows(stream);

            var row = Assert.Single(rows);
            Assert.Equal(row, Assert.Single(uploadedRows));
            Assert.Equal(2, row.RowNumber);
            Assert.True(KisMatchImportService.TryParseDate(row.Date, out var date));
            Assert.Equal(new DateOnly(2026, 10, 24), date);
        }
        finally
        {
            File.Delete(filePath);
        }
    }

    [Fact]
    public void UploadedWorkbookWithoutExportSheetIsRejected()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Other");
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<InvalidDataException>(() => KisMatchImportService.ReadRows(stream));

        Assert.Contains("Export", error.Message);
    }

    [Fact]
    public void UploadedWorkbookWithInvalidHeadersIsRejected()
    {
        using var workbook = new XLWorkbook();
        workbook.AddWorksheet("Export").Cell(1, 1).Value = "Other";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var error = Assert.Throws<InvalidDataException>(() => KisMatchImportService.ReadRows(stream));

        Assert.Contains("Datum", error.Message);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void MatchImportRequiresSeason(int seasonId, bool expectedValid)
    {
        var input = new KisMatchImportDto { SeasonId = seasonId };
        var errors = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(input, new ValidationContext(input), errors, true);

        Assert.Equal(expectedValid, valid);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    public void MatchDurationValidationRequiresPositiveMinutes(int value, bool expectedValid)
    {
        var errors = new List<ValidationResult>();
        var valid = Validator.TryValidateProperty(
            value,
            new ValidationContext(new SeasonCategoryEditDto())
            {
                MemberName = nameof(SeasonCategoryEditDto.MatchDurationMinutes),
            },
            errors);

        Assert.Equal(expectedValid, valid);
    }

    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, true)]
    public void WarmupDurationValidationAllowsZeroMinutes(int value, bool expectedValid)
    {
        var errors = new List<ValidationResult>();
        var valid = Validator.TryValidateProperty(
            value,
            new ValidationContext(new SeasonCategoryEditDto())
            {
                MemberName = nameof(SeasonCategoryEditDto.WarmupDurationMinutes),
            },
            errors);

        Assert.Equal(expectedValid, valid);
    }
}
