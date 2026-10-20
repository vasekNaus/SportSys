using Microsoft.AspNetCore.Http;
using SportSys.Razor.Services;

namespace SportSys.Razor.Tests;

public class KisImportFileValidatorTests
{
    [Fact]
    public void MissingFileReturnsValidationMessage()
    {
        Assert.Equal("Vyberte soubor XLSX.", KisImportFileValidator.GetError(null));
    }

    [Fact]
    public void EmptyFileReturnsValidationMessage()
    {
        var file = CreateFile(length: 0, fileName: "import.xlsx");

        Assert.Equal("Vyberte soubor XLSX.", KisImportFileValidator.GetError(file));
    }

    [Fact]
    public void FileLargerThanTenMiBReturnsValidationMessage()
    {
        var file = CreateFile(length: 10 * 1024 * 1024 + 1, fileName: "import.xlsx");

        Assert.Equal("Soubor může mít maximálně 10 MiB.", KisImportFileValidator.GetError(file));
    }

    [Fact]
    public void UnsupportedFileExtensionReturnsValidationMessage()
    {
        var file = CreateFile(length: 1, fileName: "import.csv");

        Assert.Equal("Vyberte soubor ve formátu XLSX.", KisImportFileValidator.GetError(file));
    }

    [Theory]
    [InlineData("import.xlsx")]
    [InlineData("import.XLSX")]
    public void XlsxFileWithinLimitIsAccepted(string fileName)
    {
        var file = CreateFile(length: 10 * 1024 * 1024, fileName);

        Assert.Null(KisImportFileValidator.GetError(file));
    }

    private static IFormFile CreateFile(long length, string fileName)
        => new FormFile(Stream.Null, 0, length, "file", fileName);
}
