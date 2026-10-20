using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SportSys.Contract.Auth;
using SportSys.Contract.Models.hr;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Database.Enums;
using SportSys.Database.Models.sport;

namespace SportSys.Razor.Tests;

public class CoachAttendanceTests
{
    private static readonly string[] Headers =
    [
        "Datum",
        "Čas od",
        "Čas do",
        "Název",
        "Typ",
        "Soupiska",
        "Účast / neúčast",
        "Poznámka",
        "Počet hodin",
    ];

    [Fact]
    public void Filter_RejectsIncompletePeriod()
    {
        var errors = Validate(new CoachAttendanceFilter { PeriodFromYear = 2026 });

        Assert.Contains(errors, error =>
            error.MemberNames.Contains(nameof(CoachAttendanceFilter.PeriodFromMonth)));
    }

    [Fact]
    public void Filter_RejectsReversedPeriodAcrossYears()
    {
        var filter = new CoachAttendanceFilter
        {
            PeriodFromYear = 2027,
            PeriodFromMonth = 1,
            PeriodToYear = 2026,
            PeriodToMonth = 12,
        };

        Assert.Contains(
            Validate(filter),
            error => error.ErrorMessage == "Období od nesmí být pozdější než období do.");
    }

    [Fact]
    public void Upload_RequiresSeason()
    {
        var upload = new CoachAttendanceUploadDto
        {
            CoachId = 1,
            PeriodYear = 2026,
            PeriodMonth = 9,
        };

        Assert.Contains(
            Validate(upload),
            error => error.MemberNames.Contains(nameof(CoachAttendanceUploadDto.SeasonId)));
    }

    [Fact]
    public void XlsxParser_ReadsTrainingAndIgnoresTrailingSummary()
    {
        var content = CreateWorkbook(sheet =>
        {
            AddTrainingRow(sheet, 2);
            sheet.Cell(3, 7).Value = "1/1";
            sheet.Cell(3, 9).Value = "1:30";
        });

        var result = CoachAttendanceXlsxParser.Parse(
            content,
            CoachAttendanceXlsxParser.CanonicalContentType,
            @"C:\uploads\dochazka.xlsx",
            2026,
            9);

        var row = Assert.Single(result.Rows);
        Assert.Equal("dochazka.xlsx", result.FileName);
        Assert.Equal(new DateOnly(2026, 9, 2), row.Date);
        Assert.Equal(new TimeOnly(16, 0), row.TimeFrom);
        Assert.Equal(new TimeOnly(17, 30), row.TimeTo);
        Assert.Equal(90, row.DurationMinutes);
        Assert.Equal(CoachAttendanceEventKind.Training, row.EventKind);
    }

    [Fact]
    public void XlsxParser_ReadsMatchWithoutEndTime()
    {
        var content = CreateWorkbook(sheet =>
        {
            sheet.Cell(2, 1).Value = new DateTime(2026, 9, 5);
            sheet.Cell(2, 2).Value = new TimeSpan(18, 10, 0);
            sheet.Cell(2, 4).Value = "HC Klatovy - HC Pilsen";
            sheet.Cell(2, 5).Value = "zápas";
            sheet.Cell(2, 6).Value = "SŽ";
            sheet.Cell(2, 7).Value = "Účast";
        });

        var row = Assert.Single(CoachAttendanceXlsxParser.Parse(
            content,
            CoachAttendanceXlsxParser.GenericBinaryContentType,
            "dochazka.xlsx",
            2026,
            9).Rows);

        Assert.Equal(CoachAttendanceEventKind.Match, row.EventKind);
        Assert.Null(row.TimeTo);
        Assert.Equal("HC Klatovy - HC Pilsen", row.Name);
    }

    [Fact]
    public void XlsxParser_RejectsDurationMismatch()
    {
        var content = CreateWorkbook(sheet =>
        {
            AddTrainingRow(sheet, 2);
            sheet.Cell(2, 9).Value = "1:00";
        });

        var exception = Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceXlsxParser.Parse(
                content,
                CoachAttendanceXlsxParser.CanonicalContentType,
                "dochazka.xlsx",
                2026,
                9));

        Assert.Contains("Řádek 2", exception.Message);
        Assert.Contains("neodpovídá", exception.Message);
    }

    [Fact]
    public void XlsxParser_RejectsDataAfterSummary()
    {
        var content = CreateWorkbook(sheet =>
        {
            AddTrainingRow(sheet, 2);
            sheet.Cell(3, 7).Value = "1/1";
            AddTrainingRow(sheet, 4);
        });

        var exception = Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceXlsxParser.Parse(
                content,
                CoachAttendanceXlsxParser.CanonicalContentType,
                "dochazka.xlsx",
                2026,
                9));

        Assert.Contains("Řádek 4", exception.Message);
        Assert.Contains("za souhrnem", exception.Message);
    }

    [Fact]
    public void Planner_ResolvesTrainingAndMatchIncludingMeetingTime()
    {
        var rows = new[]
        {
            CreateParsedRow(2, CoachAttendanceEventKind.Training),
            CreateParsedRow(
                3,
                CoachAttendanceEventKind.Match,
                timeFrom: new TimeOnly(17, 25),
                name: "HC Klatovy - HC Pilsen"),
        };

        var result = CoachAttendanceImportPlanner.Resolve(
            rows,
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 5, 31),
            [new("SŽ", "Starší žáci")],
            [
                new(
                    11,
                    new DateOnly(2026, 9, 2),
                    new TimeOnly(16, 0),
                    new TimeOnly(17, 30),
                    "SŽ",
                    (int)ETrainingState.Plan),
            ],
            [
                new(
                    22,
                    new DateOnly(2026, 9, 2),
                    new TimeOnly(18, 0),
                    "SŽ",
                    "HC Klatovy",
                    "HC Pilsen",
                    (int)EMatchState.Plan),
            ],
            [new(1, "Účast")]);

        Assert.Equal(2, result.Count);
        Assert.Equal(11, result[0].EventId);
        Assert.Equal(22, result[1].EventId);
    }

    [Fact]
    public void Planner_RejectsCancelledTrainingAndDuplicateEvent()
    {
        var row = CreateParsedRow(2, CoachAttendanceEventKind.Training);
        var exception = Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceImportPlanner.Resolve(
                [row],
                new DateOnly(2026, 8, 1),
                new DateOnly(2027, 5, 31),
                [new("SŽ", "Starší žáci")],
                [
                    new(
                        11,
                        row.Date,
                        row.TimeFrom,
                        row.TimeTo!.Value,
                        "SŽ",
                        (int)ETrainingState.Cancelled),
                ],
                [],
                [new(1, "Účast")]));

        Assert.Contains("nebyl nalezen", exception.Message);

        var duplicateException = Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceImportPlanner.Resolve(
                [row, row with { RowNumber = 3 }],
                new DateOnly(2026, 8, 1),
                new DateOnly(2027, 5, 31),
                [new("SŽ", "Starší žáci")],
                [
                    new(
                        11,
                        row.Date,
                        row.TimeFrom,
                        row.TimeTo!.Value,
                        "SŽ",
                        (int)ETrainingState.Plan),
                ],
                [],
                [new(1, "Účast")]));

        Assert.Contains("Řádky 2 a 3", duplicateException.Message);
    }

    [Fact]
    public void Model_MapsCoachMatchKeysIndexesAndDefaultConstraint()
    {
        using var db = CreateDbContext();
        var entity = db.Model.FindEntityType(typeof(CoachMatch));

        Assert.NotNull(entity);
        Assert.Equal("CoachMatch", entity.GetTableName());
        Assert.Equal("sport", entity.GetSchema());
        Assert.Equal(
            [nameof(CoachMatch.CoachId), nameof(CoachMatch.MatchId), nameof(CoachMatch.ParticipationTypeId)],
            entity.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Contains(entity.GetIndexes(), index => index.GetDatabaseName() == "IX_CoachMatch_Match");
        Assert.Contains(
            entity.GetIndexes(),
            index => index.GetDatabaseName() == "IX_CoachMatch_ParticipationType");
        Assert.Equal(
            "DF_CoachMatch_Note",
            entity.FindProperty(nameof(CoachMatch.Note))!
                .FindAnnotation("Relational:DefaultConstraintName")?.Value);
        var matchForeignKey = entity.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(Match));
        Assert.Equal(DeleteBehavior.Cascade, matchForeignKey.DeleteBehavior);
    }

    [Fact]
    public void CurrentUserIdResolver_PrefersSportSysClaim()
    {
        var principal = CreatePrincipal(
            new Claim(ClaimTypes.NameIdentifier, "999"),
            new Claim(SportSysClaimTypes.UserId, "42"));

        Assert.Equal(42, CurrentUserIdResolver.GetRequiredUserId(principal));
    }

    [Fact]
    public void CurrentUserIdResolver_RejectsMissingLocalId()
    {
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "entra-object-id"));

        Assert.Throws<CoachValidationException>(() =>
            CurrentUserIdResolver.GetRequiredUserId(principal));
    }

    private static CoachAttendanceImportRow CreateParsedRow(
        int rowNumber,
        CoachAttendanceEventKind eventKind,
        TimeOnly? timeFrom = null,
        string name = "")
    {
        return new CoachAttendanceImportRow(
            rowNumber,
            new DateOnly(2026, 9, 2),
            timeFrom ?? new TimeOnly(16, 0),
            eventKind == CoachAttendanceEventKind.Training ? new TimeOnly(17, 30) : null,
            name,
            eventKind,
            "SŽ",
            "Účast",
            string.Empty,
            eventKind == CoachAttendanceEventKind.Training ? 90 : null);
    }

    private static void AddTrainingRow(IXLWorksheet sheet, int row)
    {
        sheet.Cell(row, 1).Value = new DateTime(2026, 9, 2);
        sheet.Cell(row, 2).Value = new TimeSpan(16, 0, 0);
        sheet.Cell(row, 3).Value = new TimeSpan(17, 30, 0);
        sheet.Cell(row, 5).Value = "trénink";
        sheet.Cell(row, 6).Value = "SŽ";
        sheet.Cell(row, 7).Value = "Účast";
        sheet.Cell(row, 9).Value = new TimeSpan(1, 30, 0);
    }

    private static byte[] CreateWorkbook(Action<IXLWorksheet> configure)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("Export");
        for (var column = 1; column <= Headers.Length; column++)
            sheet.Cell(1, column).Value = Headers[column - 1];
        configure(sheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));

    private static SportSysDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<SportSysDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=SportSysCoachMatchModel;Trusted_Connection=True;",
                sqlServer => sqlServer.UseNetTopologySuite())
            .Options;
        return new SportSysDbContext(options);
    }
}
