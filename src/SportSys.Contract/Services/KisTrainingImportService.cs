using System.Globalization;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Enums;
using DbTraining = SportSys.Database.Models.sport.Training;

namespace SportSys.Contract.Services;

public sealed class KisTrainingImportService
{
    private static readonly string[] ExpectedHeaders =
    [
        "Datum",
        "Čas od",
        "Čas do",
        "Soupiska",
        "Název",
        "Místo",
        "Typ",
        "Poznámka trenéra",
    ];

    private readonly SportSysDbContext _db;
    private readonly ILogger<KisTrainingImportService> _logger;

    public KisTrainingImportService(
        SportSysDbContext db,
        ILogger<KisTrainingImportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<KisTrainingImportResult> ImportAsync(
        Stream xlsx,
        KisTrainingImportDto input,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(xlsx);
        ArgumentNullException.ThrowIfNull(input);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.SeasonId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(input.TrainingPhaseId);

        var rows = ReadRows(xlsx);
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            ct);

        if (!await _db.Seasons.AnyAsync(season => season.Id == input.SeasonId && season.IsActive, ct))
            throw new ArgumentException("Vybraná sezóna není aktivní nebo neexistuje.", nameof(input));

        if (!await _db.TrainingPhases.AnyAsync(phase => phase.Id == input.TrainingPhaseId, ct))
            throw new ArgumentException("Vybraná fáze tréninku neexistuje.", nameof(input));

        var categories = await _db.SeasonCategories
            .Where(category => category.SeasonId == input.SeasonId && category.IsActive)
            .Select(category => new CategoryLookup(category.Code, category.Name))
            .ToListAsync(ct);
        var locations = await _db.SportLocations
            .Where(location => location.IsActive)
            .Select(location => new NamedLookup(location.Id, location.Name))
            .ToListAsync(ct);
        var trainingTypeIds = await _db.TrainingTypes
            .Select(trainingType => trainingType.Id)
            .ToHashSetAsync(ct);
        var trainings = await _db.Training
            .Where(training => training.SeasonId == input.SeasonId)
            .ToListAsync(ct);

        var result = new KisTrainingImportResult();

        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();

            if (!TryParseDate(row.Date, out var date))
            {
                Skip(row, result, $"neplatné datum '{row.Date}'");
                continue;
            }

            if (!TryParseTime(row.TimeFrom, out var timeFrom))
            {
                Skip(row, result, $"neplatný čas od '{row.TimeFrom}'");
                continue;
            }

            if (!TryParseTime(row.TimeTo, out var timeTo))
            {
                Skip(row, result, $"neplatný čas do '{row.TimeTo}'");
                continue;
            }

            if (!HasValidInterval(timeFrom, timeTo))
            {
                Skip(row, result, "čas od musí být dříve než čas do");
                continue;
            }

            if (!TryResolveByName(row.Squad, categories, category => category.Name, out var category))
            {
                Skip(row, result, DescribeLookupFailure("soupiska", row.Squad, categories, item => item.Name));
                continue;
            }

            if (!TryResolveByName(row.Location, locations, location => location.Name, out var location))
            {
                Skip(row, result, DescribeLookupFailure("místo", row.Location, locations, item => item.Name));
                continue;
            }

            if (!TryMapTrainingType(row.Type, out var trainingType) ||
                !trainingTypeIds.Contains((int)trainingType))
            {
                Skip(row, result, $"neznámý typ tréninku '{row.Type}'");
                continue;
            }

            var candidates = trainings
                .Where(training =>
                    training.Date == date &&
                    training.SeasonCategoryCode.Equals(category.Code, StringComparison.OrdinalIgnoreCase) &&
                    training.LocationId == location.Id &&
                    training.TrainingTypeId == (int)trainingType)
                .ToList();

            switch (ClassifyCandidates(candidates, timeFrom, timeTo))
            {
                case KisTrainingImportAction.Confirm:
                    candidates.Single(training =>
                        training.TimeFrom == timeFrom &&
                        training.TimeTo == timeTo).TrainingStateId = (int)ETrainingState.ConfirmedKis;
                    result.Confirmed++;
                    break;

                case KisTrainingImportAction.ChangeTime:
                    var training = candidates.Single();
                    training.TimeFrom = timeFrom;
                    training.TimeTo = timeTo;
                    training.TrainingStateId = (int)ETrainingState.TimeChanged;
                    result.TimeChanged++;
                    break;

                case KisTrainingImportAction.Insert:
                    var newTraining = new DbTraining
                    {
                        Id = await GetNextSportEventIdAsync(ct),
                        SeasonId = input.SeasonId,
                        SeasonCategoryCode = category.Code,
                        Date = date,
                        TimeFrom = timeFrom,
                        TimeTo = timeTo,
                        LocationId = location.Id,
                        Note = string.Empty,
                        TrainingTypeId = (int)trainingType,
                        TrainingPhaseId = input.TrainingPhaseId,
                        TrainingStateId = (int)ETrainingState.KisOnly,
                    };
                    _db.Training.Add(newTraining);
                    trainings.Add(newTraining);
                    result.Inserted++;
                    break;

                case KisTrainingImportAction.Ambiguous:
                    Skip(row, result, "nalezeno více odpovídajících tréninků");
                    break;

                default:
                    throw new InvalidOperationException("Neznámý výsledek párování tréninku.");
            }
        }

        if (result.Confirmed > 0 || result.TimeChanged > 0 || result.Inserted > 0)
            await _db.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        _logger.LogInformation(
            "KIS import tréninků dokončen: potvrzeno {Confirmed}, změněno {TimeChanged}, vloženo {Inserted}, přeskočeno {Skipped}.",
            result.Confirmed,
            result.TimeChanged,
            result.Inserted,
            result.Skipped);

        return result;
    }

    internal static IReadOnlyList<KisTrainingImportRow> ReadRows(Stream xlsx)
    {
        using var workbook = new XLWorkbook(xlsx);
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
            .Select(row => new KisTrainingImportRow(
                row.RowNumber(),
                ReadCell(row.Cell(1)),
                ReadCell(row.Cell(2)),
                ReadCell(row.Cell(3)),
                ReadCell(row.Cell(4)),
                ReadCell(row.Cell(5)),
                ReadCell(row.Cell(6)),
                ReadCell(row.Cell(7)),
                ReadCell(row.Cell(8))))
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

    internal static bool TryMapTrainingType(string value, out ETrainingType trainingType)
    {
        trainingType = value switch
        {
            "Led" => ETrainingType.Ice,
            "Suchá příprava" => ETrainingType.Dry,
            _ => default,
        };

        return trainingType != default;
    }

    internal static bool HasValidInterval(TimeOnly timeFrom, TimeOnly timeTo)
        => timeFrom < timeTo;

    internal static KisTrainingImportAction ClassifyCandidates(
        IReadOnlyCollection<DbTraining> candidates,
        TimeOnly timeFrom,
        TimeOnly timeTo)
    {
        return candidates.Count switch
        {
            0 => KisTrainingImportAction.Insert,
            > 1 => KisTrainingImportAction.Ambiguous,
            1 when candidates.Single().TimeFrom == timeFrom &&
                   candidates.Single().TimeTo == timeTo => KisTrainingImportAction.Confirm,
            1 => KisTrainingImportAction.ChangeTime,
            _ => KisTrainingImportAction.Ambiguous,
        };
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
            ? $"neznámá {fieldName} '{sourceName}'"
            : $"nejednoznačná {fieldName} '{sourceName}'";
    }

    private static void Skip(
        KisTrainingImportRow row,
        KisTrainingImportResult result,
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
            if (_db.Database.CurrentTransaction is not null)
                command.Transaction = _db.Database.CurrentTransaction.GetDbTransaction();

            var value = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt32(value, CultureInfo.InvariantCulture);
        }
        finally
        {
            if (!wasOpen)
                await connection.CloseAsync();
        }
    }

    private sealed record CategoryLookup(string Code, string Name);

    private sealed record NamedLookup(int Id, string Name);
}

internal enum KisTrainingImportAction
{
    Confirm,
    ChangeTime,
    Insert,
    Ambiguous,
}

internal sealed record KisTrainingImportRow(
    int RowNumber,
    string Date,
    string TimeFrom,
    string TimeTo,
    string Squad,
    string Name,
    string Location,
    string Type,
    string CoachNote)
{
    public bool IsHeaderRow =>
        Date.Equals("Datum", StringComparison.Ordinal) &&
        TimeFrom.Equals("Čas od", StringComparison.Ordinal) &&
        TimeTo.Equals("Čas do", StringComparison.Ordinal);
}
