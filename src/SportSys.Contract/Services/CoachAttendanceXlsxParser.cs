using System.Globalization;
using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using SportSys.Contract.Models.hr;

namespace SportSys.Contract.Services;

public static class CoachAttendanceXlsxParser
{
  private static readonly string[] ExpectedHeaders =
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

  private static readonly CultureInfo CzechCulture = CultureInfo.GetCultureInfo("cs-CZ");
  private const long MaxMetadataEntrySizeBytes = 1024 * 1024;
  private const int MaxErrors = 50;

  public const int MaxFileSizeBytes = 10 * 1024 * 1024;
  public const string CanonicalContentType =
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
  public const string GenericBinaryContentType = "application/octet-stream";

  internal static CoachAttendanceParsedFile Parse(
      byte[] content,
      string contentType,
      string fileName,
      int periodYear,
      int periodMonth)
  {
    ArgumentNullException.ThrowIfNull(content);

    ValidateFile(content, contentType, fileName, out var safeFileName);

    try
    {
      using var stream = new MemoryStream(content, writable: false);
      using var workbook = new XLWorkbook(stream);
      var exportSheets = workbook.Worksheets
          .Where(sheet => sheet.Name.Equals("Export", StringComparison.OrdinalIgnoreCase))
          .ToList();
      if (exportSheets.Count != 1)
        throw new CoachValidationException("XLSX soubor musí obsahovat právě jeden list 'Export'.");

      var worksheet = exportSheets[0];
      ValidateHeaders(worksheet);
      var rows = ReadRows(worksheet, periodYear, periodMonth);
      return new CoachAttendanceParsedFile(safeFileName, CanonicalContentType, rows);
    }
    catch (CoachValidationException)
    {
      throw;
    }
    catch (Exception ex) when (ex is IOException
        or InvalidDataException
        or NotSupportedException
        or FormatException
        or ArgumentException
        or XmlException
        or OpenXmlPackageException)
    {
      throw new CoachValidationException(
          "Soubor docházky je poškozený nebo nemá platný XLSX formát.",
          ex);
    }
  }

  private static IReadOnlyList<CoachAttendanceImportRow> ReadRows(
      IXLWorksheet worksheet,
      int periodYear,
      int periodMonth)
  {
    var rows = new List<CoachAttendanceImportRow>();
    var errors = new List<string>();
    var summaryStarted = false;
    var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

    for (var rowNumber = 2; rowNumber <= lastRow && errors.Count < MaxErrors; rowNumber++)
    {
      var row = worksheet.Row(rowNumber);
      if (IsEmpty(row))
        continue;

      if (IsSummary(row))
      {
        summaryStarted = true;
        continue;
      }

      if (summaryStarted)
      {
        errors.Add($"Řádek {rowNumber}: datový řádek následuje za souhrnem.");
        continue;
      }

      if (TryReadRow(row, periodYear, periodMonth, out var parsedRow, out var rowErrors))
        rows.Add(parsedRow!);
      else
        errors.AddRange(rowErrors.Take(MaxErrors - errors.Count));
    }

    if (errors.Count > 0)
      throw new CoachValidationException(errors);
    if (rows.Count == 0)
      throw new CoachValidationException("List 'Export' neobsahuje žádné datové řádky.");

    return rows;
  }

  private static bool TryReadRow(
      IXLRow row,
      int periodYear,
      int periodMonth,
      out CoachAttendanceImportRow? parsedRow,
      out IReadOnlyList<string> errors)
  {
    var rowErrors = new List<string>();
    var rowNumber = row.RowNumber();
    var name = NormalizeText(ReadText(row.Cell(4)));
    var type = NormalizeText(ReadText(row.Cell(5)));
    var roster = NormalizeText(ReadText(row.Cell(6)));
    var participation = NormalizeText(ReadText(row.Cell(7)));
    var note = NormalizeText(ReadText(row.Cell(8)));

    var hasDate = TryReadDate(row.Cell(1), out var date);
    if (!hasDate)
      rowErrors.Add($"Řádek {rowNumber}: datum není platné.");
    else if (date.Year != periodYear || date.Month != periodMonth)
      rowErrors.Add($"Řádek {rowNumber}: datum neleží ve vybraném období.");

    if (!TryReadTime(row.Cell(2), out var timeFrom))
      rowErrors.Add($"Řádek {rowNumber}: čas od není platný.");

    var eventKind = type.ToLower(CzechCulture) switch
    {
      "trénink" => CoachAttendanceEventKind.Training,
      "zápas" => CoachAttendanceEventKind.Match,
      _ => (CoachAttendanceEventKind?)null,
    };
    if (eventKind is null)
      rowErrors.Add($"Řádek {rowNumber}: neznámý typ události '{type}'.");
    if (string.IsNullOrWhiteSpace(roster))
      rowErrors.Add($"Řádek {rowNumber}: soupiska je povinná.");
    if (string.IsNullOrWhiteSpace(participation))
      rowErrors.Add($"Řádek {rowNumber}: stav účasti je povinný.");
    if (note.Length > 50)
      rowErrors.Add($"Řádek {rowNumber}: poznámka může mít nejvýše 50 znaků.");

    TimeOnly? timeTo = null;
    if (eventKind == CoachAttendanceEventKind.Training)
    {
      if (!TryReadTime(row.Cell(3), out var parsedTimeTo))
        rowErrors.Add($"Řádek {rowNumber}: čas do není platný.");
      else
        timeTo = parsedTimeTo;

      if (timeTo.HasValue && timeFrom >= timeTo.Value)
        rowErrors.Add($"Řádek {rowNumber}: čas od musí být dříve než čas do.");
    }
    else if (eventKind == CoachAttendanceEventKind.Match)
    {
      if (string.IsNullOrWhiteSpace(name))
        rowErrors.Add($"Řádek {rowNumber}: název zápasu je povinný.");

      if (!row.Cell(3).IsEmpty())
      {
        if (TryReadTime(row.Cell(3), out var parsedTimeTo))
          timeTo = parsedTimeTo;
        else
          rowErrors.Add($"Řádek {rowNumber}: čas do není platný.");
      }
    }

    if (rowErrors.Count > 0)
    {
      parsedRow = null;
      errors = rowErrors;
      return false;
    }

    parsedRow = new CoachAttendanceImportRow(
        rowNumber,
        date,
        timeFrom,
        timeTo,
        name,
        eventKind!.Value,
        roster,
        participation,
        note);
    errors = [];
    return true;
  }

  private static void ValidateHeaders(IXLWorksheet worksheet)
  {
    for (var column = 1; column <= ExpectedHeaders.Length; column++)
    {
      var actual = ReadText(worksheet.Cell(1, column)).Trim();
      if (!actual.Equals(ExpectedHeaders[column - 1], StringComparison.Ordinal))
      {
        throw new CoachValidationException(
            $"Neplatná hlavička sloupce {column}: očekává se '{ExpectedHeaders[column - 1]}', nalezeno '{actual}'.");
      }
    }
  }

  private static bool TryReadDate(IXLCell cell, out DateOnly date)
  {
    if (cell.TryGetValue<DateTime>(out var dateTime))
    {
      date = DateOnly.FromDateTime(dateTime);
      return true;
    }

    return DateOnly.TryParseExact(
        ReadText(cell).Trim(),
        ["d.M.yyyy", "dd.MM.yyyy", "d. M. yyyy", "dd. MM. yyyy", "yyyy-MM-dd"],
        CzechCulture,
        DateTimeStyles.AllowWhiteSpaces,
        out date);
  }

  private static bool TryReadTime(IXLCell cell, out TimeOnly time)
  {
    if (cell.TryGetValue<TimeSpan>(out var timeSpan)
        && timeSpan >= TimeSpan.Zero
        && timeSpan < TimeSpan.FromDays(1))
    {
      time = TimeOnly.FromTimeSpan(timeSpan);
      return true;
    }
    if (cell.TryGetValue<DateTime>(out var dateTime))
    {
      time = TimeOnly.FromDateTime(dateTime);
      return true;
    }

    return TimeOnly.TryParseExact(
        ReadText(cell).Trim(),
        ["H:mm", "HH:mm"],
        CzechCulture,
        DateTimeStyles.None,
        out time);
  }

  private static bool TryReadDurationMinutes(IXLCell cell, out int minutes)
  {
    if (cell.TryGetValue<TimeSpan>(out var duration)
        && duration >= TimeSpan.Zero
        && duration.TotalMinutes <= int.MaxValue
        && duration.TotalMinutes == Math.Truncate(duration.TotalMinutes))
    {
      minutes = checked((int)duration.TotalMinutes);
      return true;
    }

    var value = ReadText(cell).Trim();
    var parts = value.Split(':');
    if (parts.Length == 2
        && int.TryParse(parts[0], NumberStyles.None, CzechCulture, out var hours)
        && int.TryParse(parts[1], NumberStyles.None, CzechCulture, out var minutePart)
        && hours >= 0
        && minutePart is >= 0 and < 60)
    {
      minutes = checked(hours * 60 + minutePart);
      return true;
    }

    minutes = default;
    return false;
  }

  private static string NormalizeText(string value) =>
      string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

  private static string ReadText(IXLCell cell) => cell.GetFormattedString(CzechCulture);

  private static bool IsEmpty(IXLRow row) =>
      Enumerable.Range(1, ExpectedHeaders.Length).All(column => row.Cell(column).IsEmpty());

  private static bool IsSummary(IXLRow row) =>
      Enumerable.Range(1, 6).All(column => row.Cell(column).IsEmpty())
      && row.Cell(8).IsEmpty()
      && (!row.Cell(7).IsEmpty() || !row.Cell(9).IsEmpty());

  private static void ValidateFile(
      byte[] content,
      string contentType,
      string fileName,
      out string safeFileName)
  {
    if (content.Length == 0)
      throw new CoachValidationException("Soubor docházky je prázdný.");
    if (content.Length > MaxFileSizeBytes)
      throw new CoachValidationException("Soubor docházky nesmí být větší než 10 MiB.");

    safeFileName = GetSafeFileName(fileName);
    if (!Path.GetExtension(safeFileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
      throw new CoachValidationException("Soubor docházky musí mít příponu .xlsx.");

    var normalizedContentType = contentType?.Trim();
    if (!string.Equals(normalizedContentType, CanonicalContentType, StringComparison.OrdinalIgnoreCase)
        && !string.Equals(normalizedContentType, GenericBinaryContentType, StringComparison.OrdinalIgnoreCase))
    {
      throw new CoachValidationException("Soubor docházky nemá povolený typ obsahu.");
    }

    ValidatePackage(content);
  }

  private static string GetSafeFileName(string fileName)
  {
    var normalizedSeparators = fileName?.Trim().Replace('\\', '/');
    var safeFileName = Path.GetFileName(normalizedSeparators);
    if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Length > 255)
      throw new CoachValidationException("Název souboru docházky je neplatný.");

    return safeFileName;
  }

  private static void ValidatePackage(byte[] content)
  {
    if (content.Length < 4
        || content[0] != 0x50
        || content[1] != 0x4B
        || content[2] != 0x03
        || content[3] != 0x04)
    {
      throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
    }

    try
    {
      using var stream = new MemoryStream(content, writable: false);
      using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: false);
      ValidateContentTypes(ReadXml(GetRequiredEntry(archive, "[Content_Types].xml")));
      ValidateRelationships(ReadXml(GetRequiredEntry(archive, "_rels/.rels")));
      ValidateWorkbook(ReadXml(GetRequiredEntry(archive, "xl/workbook.xml")));
    }
    catch (CoachValidationException)
    {
      throw;
    }
    catch (Exception ex) when (ex is IOException
        or InvalidDataException
        or NotSupportedException
        or XmlException)
    {
      throw new CoachValidationException(
          "Soubor docházky je poškozený nebo není platný XLSX.",
          ex);
    }
  }

  private static ZipArchiveEntry GetRequiredEntry(ZipArchive archive, string entryName)
  {
    var entry = archive.Entries.FirstOrDefault(
        candidate => candidate.FullName.Equals(entryName, StringComparison.Ordinal));
    if (entry is null || entry.Length <= 0 || entry.Length > MaxMetadataEntrySizeBytes)
      throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
    return entry;
  }

  private static XDocument ReadXml(ZipArchiveEntry entry)
  {
    var settings = new XmlReaderSettings
    {
      DtdProcessing = DtdProcessing.Prohibit,
      XmlResolver = null,
      MaxCharactersInDocument = MaxMetadataEntrySizeBytes,
    };
    using var stream = entry.Open();
    using var reader = XmlReader.Create(stream, settings);
    return XDocument.Load(reader, System.Xml.Linq.LoadOptions.None);
  }

  private static void ValidateContentTypes(XDocument document)
  {
    if (document.Root?.Name.LocalName != "Types")
    {
      throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
    }
  }

  private static void ValidateRelationships(XDocument document)
  {
    if (document.Root?.Name.LocalName != "Relationships")
    {
      throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
    }
  }

  private static void ValidateWorkbook(XDocument document)
  {
    if (document.Root?.Name.LocalName != "workbook")
      throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
  }
}
