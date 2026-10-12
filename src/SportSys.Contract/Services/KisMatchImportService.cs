using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Enums;
using DbMatch = SportSys.Database.Models.sport.Match;

namespace SportSys.Contract.Services;

public sealed class KisMatchImportService
{
    private static readonly string[] ExpectedHeaders =
    [
        "Datum",
        "Čas",
        "Soupiska",
        "Domácí",
        "Hosté",
        "Místo",
        "Typ",
    ];

    private readonly SportSysDbContext _db;
    private readonly ILogger<KisMatchImportService> _logger;

    public KisMatchImportService(
        SportSysDbContext db,
        ILogger<KisMatchImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<KisMatchImportResult> ImportAsync(
        string xlsxFilePath,
        int seasonId,
        CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xlsxFilePath);

        if (!File.Exists(xlsxFilePath))
            throw new FileNotFoundException($"XLSX soubor nenalezen: {xlsxFilePath}", xlsxFilePath);

        var categories = await _db.SeasonCategories
            .Where(category => category.SeasonId == seasonId)
            .Select(category => new CategoryLookup(
                category.Code,
                category.Name,
                category.MatchDurationMinutes))
            .ToListAsync(ct);

        var teams = await _db.Teams
            .Select(team => new NamedLookup(team.Id, team.Name))
            .ToListAsync(ct);

        var locations = await _db.SportLocations
            .Select(location => new NamedLookup(location.Id, location.Name))
            .ToListAsync(ct);

        var matchTypeIds = await _db.MatchTypes
            .Select(matchType => matchType.Id)
            .ToHashSetAsync(ct);

        var existingMatches = await _db.Matches
            .Where(match => match.SeasonId == seasonId)
            .Select(match => new KisMatchIdentity(
                match.LocationId,
                match.Date,
                match.TimeFrom,
                match.SeasonCategoryCode,
                match.HomeTeamId,
                match.AwayTeamId))
            .ToListAsync(ct);

        var result = new KisMatchImportResult();

        foreach (var row in ReadRows(xlsxFilePath))
        {
            ct.ThrowIfCancellationRequested();

            if (!TryParseDate(row.Date, out var date))
            {
                Skip(row, result, $"neplatné datum '{row.Date}'");
                continue;
            }

            if (!TryParseTime(row.Time, out var timeFrom))
            {
                Skip(row, result, $"neplatný čas '{row.Time}'");
                continue;
            }

            if (!TryResolveByName(row.Squad, categories, category => category.Name, out var category))
            {
                Skip(row, result, DescribeLookupFailure("soupiska", row.Squad, categories, item => item.Name));
                continue;
            }

            if (category.MatchDurationMinutes <= 0)
            {
                Skip(row, result, $"kategorie '{category.Name}' nemá kladnou délku zápasu");
                continue;
            }

            if (!TryResolveByName(row.HomeTeam, teams, team => team.Name, out var homeTeam))
            {
                Skip(row, result, DescribeLookupFailure("domácí tým", row.HomeTeam, teams, item => item.Name));
                continue;
            }

            if (!TryResolveByName(row.AwayTeam, teams, team => team.Name, out var awayTeam))
            {
                Skip(row, result, DescribeLookupFailure("hostující tým", row.AwayTeam, teams, item => item.Name));
                continue;
            }

            if (!TryResolveByName(row.Location, locations, location => location.Name, out var location))
            {
                Skip(row, result, DescribeLookupFailure("místo", row.Location, locations, item => item.Name));
                continue;
            }

            if (!TryMapMatchType(row.Type, out var matchType) ||
                !matchTypeIds.Contains((int)matchType))
            {
                Skip(row, result, $"neznámý typ zápasu '{row.Type}'");
                continue;
            }

            var sameStartMatches = existingMatches
                .Where(match =>
                    match.LocationId == location.Id &&
                    match.Date == date &&
                    match.TimeFrom == timeFrom)
                .ToList();

            var identity = new KisMatchIdentity(
                location.Id,
                date,
                timeFrom,
                category.Code,
                homeTeam.Id,
                awayTeam.Id);

            var matchingMatches = sameStartMatches
                .Where(match => IsEquivalent(identity, match))
                .ToList();

            if (matchingMatches.Count == 1)
            {
                result.Existing++;
                continue;
            }

            if (matchingMatches.Count > 1)
            {
                Skip(row, result, "nalezeno více shodných zápasů");
                continue;
            }

            var match = new DbMatch
            {
                Id = await GetNextSportEventIdAsync(ct),
                SeasonId = seasonId,
                SeasonCategoryCode = category.Code,
                Date = date,
                TimeFrom = timeFrom,
                TimeTo = ResolveTimeTo(timeFrom, category.MatchDurationMinutes),
                LocationId = location.Id,
                Note = string.Empty,
                HomeTeamId = homeTeam.Id,
                AwayTeamId = awayTeam.Id,
                MatchTypeId = (int)matchType,
            };

            _db.Matches.Add(match);
            existingMatches.Add(new KisMatchIdentity(
                location.Id,
                date,
                timeFrom,
                category.Code,
                homeTeam.Id,
                awayTeam.Id));
            result.Inserted++;
        }

        if (result.Inserted > 0)
            await _db.SaveChangesAsync(ct);

        _logger.LogInformation(
            "KIS import dokončen: vloženo {Inserted}, existující {Existing}, přeskočeno {Skipped}.",
            result.Inserted,
            result.Existing,
            result.Skipped);

        return result;
    }

    internal static IReadOnlyList<KisMatchImportRow> ReadRows(string xlsxFilePath)
    {
        using var workbook = new XLWorkbook(xlsxFilePath);
        var worksheet = workbook.Worksheets
            .FirstOrDefault(sheet => sheet.Name.Equals("Export", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidDataException("XLSX soubor neobsahuje list 'Export'.");

        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidDataException("List 'Export' neobsahuje hlavičku.");

        for (var column = 1; column <= ExpectedHeaders.Length; column++)
        {
            var header = ReadCell(headerRow.Cell(column));
            if (!header.Equals(ExpectedHeaders[column - 1], StringComparison.Ordinal))
            {
                throw new InvalidDataException(
                    $"Neplatná hlavička sloupce {column}: očekává se '{ExpectedHeaders[column - 1]}', nalezeno '{header}'.");
            }
        }

        return worksheet.RowsUsed()
            .Where(row => row.RowNumber() > headerRow.RowNumber())
            .Select(row => new KisMatchImportRow(
                row.RowNumber(),
                ReadCell(row.Cell(1)),
                ReadCell(row.Cell(2)),
                ReadCell(row.Cell(3)),
                ReadCell(row.Cell(4)),
                ReadCell(row.Cell(5)),
                ReadCell(row.Cell(6)),
                ReadCell(row.Cell(7))))
            .Where(row => !row.IsHeaderRow)
            .ToList();
    }

    internal static bool TryParseDate(string value, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            value,
            ["d.M.yyyy", "dd.MM.yyyy", "yyyy-MM-dd"],
            CultureInfo.GetCultureInfo("cs-CZ"),
            DateTimeStyles.None,
            out date);
    }

    internal static bool TryParseTime(string value, out TimeOnly time)
    {
        return TimeOnly.TryParseExact(
            value,
            ["H:mm", "HH:mm"],
            CultureInfo.GetCultureInfo("cs-CZ"),
            DateTimeStyles.None,
            out time);
    }

    internal static bool TryMapMatchType(string value, out EMatchType matchType)
    {
        matchType = value switch
        {
            "Ligový zápas" => EMatchType.League,
            "Příprava" => EMatchType.Friendly,
            "Turnaj" => EMatchType.Tournament,
            _ => default,
        };

        return matchType != default;
    }

    internal static TimeOnly ResolveTimeTo(TimeOnly timeFrom, int matchDurationMinutes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(matchDurationMinutes);
        return timeFrom.AddMinutes(matchDurationMinutes);
    }

    internal static bool IsEquivalent(
        KisMatchIdentity expected,
        KisMatchIdentity candidate)
    {
        return expected.LocationId == candidate.LocationId &&
               expected.Date == candidate.Date &&
               expected.TimeFrom == candidate.TimeFrom &&
               expected.SeasonCategoryCode.Equals(
                   candidate.SeasonCategoryCode,
                   StringComparison.OrdinalIgnoreCase) &&
               expected.HomeTeamId == candidate.HomeTeamId &&
               expected.AwayTeamId == candidate.AwayTeamId;
    }

    private static string ReadCell(IXLCell cell) => cell.GetFormattedString().Trim();

    private static bool TryResolveByName<T>(
        string sourceName,
        IEnumerable<T> values,
        Func<T, string> nameSelector,
        out T result)
        where T : class
    {
        var matches = values
            .Where(value => nameSelector(value).Equals(sourceName, StringComparison.OrdinalIgnoreCase))
            .Take(2)
            .ToList();

        result = matches.Count == 1 ? matches[0] : null!;
        return matches.Count == 1;
    }

    private static string DescribeLookupFailure<T>(
        string fieldName,
        string sourceName,
        IEnumerable<T> values,
        Func<T, string> nameSelector)
    {
        var count = values.Count(value =>
            nameSelector(value).Equals(sourceName, StringComparison.OrdinalIgnoreCase));

        return count == 0
            ? $"neznámý {fieldName} '{sourceName}'"
            : $"nejednoznačný {fieldName} '{sourceName}'";
    }

    private static void Skip(
        KisMatchImportRow row,
        KisMatchImportResult result,
        string message)
    {
        result.Skipped++;
        result.AddWarning(row.RowNumber, message);
    }

    private async Task<int> GetNextSportEventIdAsync(CancellationToken ct)
    {
        var connection = _db.Database.GetDbConnection();
        var wasOpen = connection.State == System.Data.ConnectionState.Open;

        if (!wasOpen)
            await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT NEXT VALUE FOR [sport].[SportEventSeq]";
            var value = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }

    private sealed record CategoryLookup(
        string Code,
        string Name,
        int MatchDurationMinutes);

    private sealed record NamedLookup(int Id, string Name);

}

internal sealed record KisMatchIdentity(
    int LocationId,
    DateOnly Date,
    TimeOnly TimeFrom,
    string SeasonCategoryCode,
    int HomeTeamId,
    int AwayTeamId);

internal sealed record KisMatchImportRow(
    int RowNumber,
    string Date,
    string Time,
    string Squad,
    string HomeTeam,
    string AwayTeam,
    string Location,
    string Type)
{
    public bool IsHeaderRow =>
        Date.Equals("Datum", StringComparison.Ordinal) &&
        Time.Equals("Čas", StringComparison.Ordinal);
}
