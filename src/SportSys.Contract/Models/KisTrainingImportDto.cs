using System.ComponentModel.DataAnnotations;

namespace SportSys.Contract.Models;

public sealed class KisTrainingImportDto
{
    [Range(1, int.MaxValue, ErrorMessage = "Sezóna je povinná.")]
    [Display(Name = "Sezóna")]
    public int SeasonId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Fáze tréninku je povinná.")]
    [Display(Name = "Fáze pro nové tréninky")]
    public int TrainingPhaseId { get; set; }
}

public sealed class KisTrainingImportResult
{
    public int Confirmed { get; set; }
    public int TimeChanged { get; set; }
    public int Inserted { get; set; }
    public int Skipped { get; set; }
    public List<KisTrainingImportWarning> Warnings { get; } = [];

    public void AddWarning(int rowNumber, string message)
    {
        Warnings.Add(new KisTrainingImportWarning(rowNumber, message));
    }
}

public sealed record KisTrainingImportWarning(int RowNumber, string Message);
