namespace SportSys.Razor.Services;

public static class KisImportFileValidator
{
    public static string? GetError(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return "Vyberte soubor XLSX.";
        if (file.Length > 10 * 1024 * 1024)
            return "Soubor může mít maximálně 10 MiB.";
        if (!Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            return "Vyberte soubor ve formátu XLSX.";

        return null;
    }
}
