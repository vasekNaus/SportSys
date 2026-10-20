namespace SportSys.Contract.Models;

public sealed class KisMatchImportResult
{
    public int Inserted { get; set; }
    public int Existing { get; set; }
    public int Skipped { get; set; }
    public List<KisMatchImportWarning> Warnings { get; } = [];

    public void AddWarning(int rowNumber, string message)
    {
        Warnings.Add(new KisMatchImportWarning(rowNumber, message));
    }
}

public sealed record KisMatchImportWarning(int RowNumber, string Message);
