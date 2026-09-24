using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Security.Claims;
using SportSys.Contract.Auth;
using SportSys.Contract.Models.hr;
using SportSys.Contract.Services;

namespace SportSys.Razor.Tests;

public class CoachAttendanceTests
{
    [Fact]
    public void Filter_RejectsIncompletePeriod()
    {
        var filter = new CoachAttendanceFilter
        {
            PeriodFromYear = 2026,
        };

        var errors = Validate(filter);

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

        var errors = Validate(filter);

        Assert.Contains(errors, error =>
            error.ErrorMessage == "Období od nesmí být pozdější než období do.");
    }

    [Fact]
    public void Filter_AcceptsDecemberToJanuaryRange()
    {
        var filter = new CoachAttendanceFilter
        {
            PeriodFromYear = 2026,
            PeriodFromMonth = 12,
            PeriodToYear = 2027,
            PeriodToMonth = 1,
        };

        Assert.Empty(Validate(filter));
    }

    [Fact]
    public void Upload_RejectsInvalidMonth()
    {
        var upload = new CoachAttendanceUploadDto
        {
            CoachId = 1,
            PeriodYear = 2026,
            PeriodMonth = 13,
        };

        Assert.Contains(
            Validate(upload),
            error => error.MemberNames.Contains(nameof(CoachAttendanceUploadDto.PeriodMonth)));
    }

    [Fact]
    public void XlsxValidator_AcceptsValidPackageAndSanitizesFileName()
    {
        var content = CreateXlsxPackage();

        var result = CoachAttendanceXlsxValidator.Validate(
            content,
            CoachAttendanceXlsxValidator.CanonicalContentType,
            @"C:\uploads\dochazka.xlsx");

        Assert.Equal("dochazka.xlsx", result.FileName);
        Assert.Equal(CoachAttendanceXlsxValidator.CanonicalContentType, result.ContentType);
    }

    [Fact]
    public void XlsxValidator_RejectsRenamedZipWithoutWorkbook()
    {
        var content = CreateZipPackage("[Content_Types].xml");

        var exception = Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceXlsxValidator.Validate(
                content,
                CoachAttendanceXlsxValidator.GenericBinaryContentType,
                "dochazka.xlsx"));

        Assert.Contains("XLSX strukturu", exception.Message);
    }

    [Fact]
    public void XlsxValidator_RejectsFakeXmlEntries()
    {
        var content = CreateZipPackage(
            ("[Content_Types].xml", "<xml />"),
            ("_rels/.rels", "<xml />"),
            ("xl/workbook.xml", "<xml />"));

        Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceXlsxValidator.Validate(
                content,
                CoachAttendanceXlsxValidator.CanonicalContentType,
                "dochazka.xlsx"));
    }

    [Fact]
    public void XlsxValidator_RejectsWrongExtension()
    {
        var content = CreateXlsxPackage();

        Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceXlsxValidator.Validate(
                content,
                CoachAttendanceXlsxValidator.CanonicalContentType,
                "dochazka.xls"));
    }

    [Fact]
    public void XlsxValidator_RejectsTruncatedZipAsValidationError()
    {
        var content = CreateXlsxPackage();
        Array.Resize(ref content, content.Length / 2);

        Assert.Throws<CoachValidationException>(() =>
            CoachAttendanceXlsxValidator.Validate(
                content,
                CoachAttendanceXlsxValidator.CanonicalContentType,
                "dochazka.xlsx"));
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
    public void CurrentUserIdResolver_UsesNumericNameIdentifier()
    {
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "23"));

        Assert.Equal(23, CurrentUserIdResolver.GetRequiredUserId(principal));
    }

    [Fact]
    public void CurrentUserIdResolver_RejectsMissingLocalId()
    {
        var principal = CreatePrincipal(new Claim(ClaimTypes.NameIdentifier, "entra-object-id"));

        Assert.Throws<CoachValidationException>(() =>
            CurrentUserIdResolver.GetRequiredUserId(principal));
    }

    private static List<ValidationResult> Validate(object model)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(model, new ValidationContext(model), results, true);
        return results;
    }

    private static byte[] CreateXlsxPackage() =>
        CreateZipPackage(
            (
                "[Content_Types].xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
                  <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
                </Types>
                """),
            (
                "_rels/.rels",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml" />
                </Relationships>
                """),
            (
                "xl/workbook.xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" />
                """));

    private static byte[] CreateZipPackage(params string[] entryNames)
    {
        return CreateZipPackage(entryNames.Select(name => (name, "<xml />")).ToArray());
    }

    private static byte[] CreateZipPackage(params (string Name, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entryData in entries)
            {
                var entry = archive.CreateEntry(entryData.Name);
                using var writer = new StreamWriter(entry.Open());
                writer.Write(entryData.Content);
            }
        }

        return stream.ToArray();
    }

    private static ClaimsPrincipal CreatePrincipal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "Test"));
}
