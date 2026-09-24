using ClosedXML.Excel;
using SportSys.Contract.Models;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Services;

public sealed class TrainingScheduleExcelExporter
{
    private const string WorksheetName = "Tréninky";
    private const string TableName = "TrainingSchedule";
    private const double MaximumColumnWidth = 50;

    public byte[] Export(IReadOnlyCollection<TrainingScheduleItemDto> trainings)
    {
        ArgumentOutOfRangeException.ThrowIfZero(trainings.Count);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(WorksheetName);
        WriteHeader(worksheet);

        var rowNumber = 2;
        foreach (var dateGroup in trainings
            .GroupBy(training => training.Date)
            .OrderBy(group => group.Key))
        {
            var blocks = TrainingScheduleBlockFactory.CreateBlocks(
                dateGroup.Cast<ITrainingScheduleItem>().ToList());

            foreach (var block in blocks)
            {
                WriteRow(worksheet, rowNumber, dateGroup.Key, block);
                rowNumber++;
            }
        }

        var tableRange = worksheet.Range(1, 1, rowNumber - 1, 8);
        tableRange.CreateTable(TableName);

        worksheet.Column(2).Style.DateFormat.Format = "dd.MM.yyyy";
        worksheet.Columns(3, 4).Style.DateFormat.Format = "hh:mm";
        worksheet.Columns(5, 8).Style.Alignment.WrapText = true;
        worksheet.Columns().AdjustToContents();

        foreach (var column in worksheet.ColumnsUsed())
        {
            if (column.Width > MaximumColumnWidth)
                column.Width = MaximumColumnWidth;
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteHeader(IXLWorksheet worksheet)
    {
        worksheet.Cell(1, 1).Value = "Kategorie";
        worksheet.Cell(1, 2).Value = "Datum";
        worksheet.Cell(1, 3).Value = "Čas od";
        worksheet.Cell(1, 4).Value = "Čas do";
        worksheet.Cell(1, 5).Value = "Typ tréninku";
        worksheet.Cell(1, 6).Value = "Lokalita";
        worksheet.Cell(1, 7).Value = "Trenéři";
        worksheet.Cell(1, 8).Value = "Stav";
    }

    private static void WriteRow(
        IXLWorksheet worksheet,
        int rowNumber,
        DateOnly date,
        TrainingScheduleBlockData block)
    {
        worksheet.Cell(rowNumber, 1).Value = block.Title;
        worksheet.Cell(rowNumber, 2).Value = date.ToDateTime(TimeOnly.MinValue);
        worksheet.Cell(rowNumber, 3).Value = block.TimeFrom.ToTimeSpan();
        worksheet.Cell(rowNumber, 4).Value = block.TimeTo.ToTimeSpan();
        worksheet.Cell(rowNumber, 5).Value = block.TrainingTypeSummary;
        worksheet.Cell(rowNumber, 6).Value = block.LocationSummary;
        worksheet.Cell(rowNumber, 7).Value = block.CoachSummary;
        worksheet.Cell(rowNumber, 8).Value = block.UniformStateName ?? string.Empty;
    }
}
