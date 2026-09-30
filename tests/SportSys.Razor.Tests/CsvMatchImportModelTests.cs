using SportSys.Contract.Services;

namespace SportSys.Razor.Tests;

public class CsvMatchImportModelTests
{
    [Fact]
    public void ImportedMatchWithoutKnownEndUsesStartAsEnd()
    {
        var timeFrom = new TimeOnly(17, 30);
        var timeTo = CsvMatchImportService.ResolveImportedTimeTo(timeFrom);

        Assert.Equal(timeFrom, timeTo);
    }
}
