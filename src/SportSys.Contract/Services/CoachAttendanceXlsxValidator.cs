using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
using SportSys.Contract.Models.hr;

namespace SportSys.Contract.Services;

public static class CoachAttendanceXlsxValidator
{
    private static readonly XNamespace ContentTypesNamespace =
        "http://schemas.openxmlformats.org/package/2006/content-types";
    private static readonly XNamespace RelationshipsNamespace =
        "http://schemas.openxmlformats.org/package/2006/relationships";
    private static readonly XNamespace SpreadsheetNamespace =
        "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    private const string WorkbookContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml";
    private const string OfficeDocumentRelationshipType =
        "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument";
    private const long MaxMetadataEntrySizeBytes = 1024 * 1024;

    public const int MaxFileSizeBytes = 10 * 1024 * 1024;
    public const string CanonicalContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    public const string GenericBinaryContentType = "application/octet-stream";

    public static CoachAttendanceXlsxValidationResult Validate(
        byte[] content,
        string contentType,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
            throw new CoachValidationException("Soubor docházky je prázdný.");
        if (content.Length > MaxFileSizeBytes)
            throw new CoachValidationException("Soubor docházky nesmí být větší než 10 MiB.");

        var safeFileName = GetSafeFileName(fileName);
        if (!string.Equals(Path.GetExtension(safeFileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new CoachValidationException("Soubor docházky musí mít příponu .xlsx.");

        var normalizedContentType = contentType?.Trim();
        if (!string.Equals(normalizedContentType, CanonicalContentType, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(normalizedContentType, GenericBinaryContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new CoachValidationException("Soubor docházky nemá povolený typ obsahu.");
        }

        ValidatePackage(content);
        return new CoachAttendanceXlsxValidationResult(safeFileName, CanonicalContentType);
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

            var contentTypesEntry = GetRequiredEntry(archive, "[Content_Types].xml");
            var relationshipsEntry = GetRequiredEntry(archive, "_rels/.rels");
            var workbookEntry = GetRequiredEntry(archive, "xl/workbook.xml");

            ValidateContentTypes(ReadXml(contentTypesEntry));
            ValidateRelationships(ReadXml(relationshipsEntry));
            ValidateWorkbook(ReadXml(workbookEntry));
        }
        catch (CoachValidationException)
        {
            throw;
        }
        catch (IOException ex)
        {
            throw new CoachValidationException("Soubor docházky je poškozený nebo není platný XLSX.", ex);
        }
        catch (InvalidDataException ex)
        {
            throw new CoachValidationException("Soubor docházky je poškozený nebo není platný XLSX.", ex);
        }
        catch (NotSupportedException ex)
        {
            throw new CoachValidationException("Soubor docházky je poškozený nebo není platný XLSX.", ex);
        }
        catch (XmlException ex)
        {
            throw new CoachValidationException("Soubor docházky obsahuje neplatná XML metadata.", ex);
        }
    }

    private static ZipArchiveEntry GetRequiredEntry(ZipArchive archive, string entryName)
    {
        var entry = archive.Entries.FirstOrDefault(
            candidate => string.Equals(candidate.FullName, entryName, StringComparison.Ordinal));
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
        return XDocument.Load(reader, LoadOptions.None);
    }

    private static void ValidateContentTypes(XDocument document)
    {
        if (document.Root?.Name != ContentTypesNamespace + "Types"
            || !document.Root.Elements(ContentTypesNamespace + "Override").Any(element =>
                string.Equals((string?)element.Attribute("PartName"), "/xl/workbook.xml", StringComparison.Ordinal)
                && string.Equals((string?)element.Attribute("ContentType"), WorkbookContentType, StringComparison.Ordinal)))
        {
            throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
        }
    }

    private static void ValidateRelationships(XDocument document)
    {
        if (document.Root?.Name != RelationshipsNamespace + "Relationships"
            || !document.Root.Elements(RelationshipsNamespace + "Relationship").Any(element =>
                string.Equals((string?)element.Attribute("Type"), OfficeDocumentRelationshipType, StringComparison.Ordinal)
                && ((string?)element.Attribute("Target"))?.TrimStart('/') == "xl/workbook.xml"))
        {
            throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
        }
    }

    private static void ValidateWorkbook(XDocument document)
    {
        if (document.Root?.Name != SpreadsheetNamespace + "workbook")
            throw new CoachValidationException("Soubor docházky nemá platnou XLSX strukturu.");
    }
}

public sealed record CoachAttendanceXlsxValidationResult(
    string FileName,
    string ContentType);
