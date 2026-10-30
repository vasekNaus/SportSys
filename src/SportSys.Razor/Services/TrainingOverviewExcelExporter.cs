using ClosedXML.Excel;
using SportSys.Contract.Models;
using SportSys.Razor.Models;
using SportSys.Razor.Models.TrainingSchedule;

namespace SportSys.Razor.Services;

public sealed class TrainingOverviewExcelExporter
{
    public const string ContentType =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const double MaximumColumnWidth = 50;

    public byte[] ExportRequirements(IReadOnlyCollection<TrainingRequirementListItem> requirements)
        => Export(
            "Požadavky",
            [
                "Sezóna",
                "Kategorie",
                "Typ tréninku",
                "Fáze",
                "Od",
                "Do",
                "Rozsah [h]",
                "Hlavní",
                "Asistent",
                "Brankáři",
                "Obránci",
                "Trenéři",
            ],
            requirements,
            WriteRequirement);

    public byte[] ExportPlans(
        IReadOnlyCollection<TrainingPlanScheduleItemDto> plans,
        string seasonName)
        => Export(
            "Plány",
            [
                "Sezóna",
                "Kategorie",
                "Typ tréninku",
                "Fáze",
                "Lokalita",
                "Od",
                "Do",
                "Název",
                "Den v týdnu",
                "Čas od",
                "Čas do",
                "Doba trvání",
                "Trenéři",
            ],
            plans,
            (worksheet, rowNumber, plan) => WritePlan(worksheet, rowNumber, plan, seasonName));

    public byte[] ExportCoverage(IReadOnlyCollection<TrainingRequirementCoverageRow> rows)
        => Export(
            "Pokrytí",
            [
                "Sezóna",
                "Kategorie",
                "Typ tréninku",
                "Trenér",
                "Role",
                "Požadavek [h]",
                "Plán [h]",
                "Status",
            ],
            rows,
            WriteCoverage);

    private static byte[] Export<T>(
        string worksheetName,
        IReadOnlyList<string> headers,
        IReadOnlyCollection<T> rows,
        Action<IXLWorksheet, int, T> writeRow)
    {
        ArgumentNullException.ThrowIfNull(rows);
        ArgumentOutOfRangeException.ThrowIfZero(rows.Count);

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(worksheetName);

        for (var column = 0; column < headers.Count; column++)
            worksheet.Cell(1, column + 1).Value = headers[column];

        var rowNumber = 2;
        foreach (var row in rows)
        {
            writeRow(worksheet, rowNumber, row);
            rowNumber++;
        }

        var range = worksheet.Range(1, 1, rowNumber - 1, headers.Count);
        range.Row(1).Style.Font.Bold = true;
        range.SetAutoFilter();
        worksheet.SheetView.FreezeRows(1);
        for (var index = 0; index < headers.Count; index++)
        {
            var column = worksheet.Column(index + 1);
            switch (headers[index])
            {
                case "Od":
                case "Do":
                    column.Style.DateFormat.Format = "dd.MM.yyyy";
                    break;
                case "Čas od":
                case "Čas do":
                    column.Style.DateFormat.Format = "hh:mm";
                    break;
                case "Rozsah [h]":
                case "Požadavek [h]":
                case "Plán [h]":
                    column.Style.NumberFormat.Format = "0.##";
                    break;
                case "Doba trvání":
                    column.Style.NumberFormat.Format = "0 \"min\"";
                    break;
            }
        }

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

    private static void WriteRequirement(
        IXLWorksheet worksheet,
        int rowNumber,
        TrainingRequirementListItem requirement)
    {
        worksheet.Cell(rowNumber, 1).Value = requirement.SeasonName;
        worksheet.Cell(rowNumber, 2).Value = requirement.SeasonCategoryName;
        worksheet.Cell(rowNumber, 3).Value = requirement.TrainingTypeName;
        worksheet.Cell(rowNumber, 4).Value = requirement.TrainingPhaseName;
        worksheet.Cell(rowNumber, 5).Value = requirement.From.ToDateTime(TimeOnly.MinValue);
        worksheet.Cell(rowNumber, 6).Value = requirement.To.ToDateTime(TimeOnly.MinValue);
        worksheet.Cell(rowNumber, 7).Value = requirement.DurationHours;

        var roleCodes = new[] { "H", "A", "B", "O" };
        for (var index = 0; index < roleCodes.Length; index++)
        {
            var coaches = RequirementCoachGroups.ForRole(
                requirement.CoachAssignments,
                roleCodes[index]);
            worksheet.Cell(rowNumber, index + 8).Value =
                coaches.Count == 0 ? "-" : string.Join(", ", coaches);
        }

        var otherRoles = RequirementCoachGroups.Group(
            RequirementCoachGroups.OtherRoles(requirement.CoachAssignments),
            includeHours: true);
        worksheet.Cell(rowNumber, 12).Value = otherRoles.Count == 0
            ? "-"
            : string.Join(
                Environment.NewLine,
                otherRoles.Select(group =>
                    $"{group.RoleName}: {string.Join(", ", group.CoachDisplayTexts)}"));
    }

    private static void WritePlan(
        IXLWorksheet worksheet,
        int rowNumber,
        TrainingPlanScheduleItemDto plan,
        string seasonName)
    {
        worksheet.Cell(rowNumber, 1).Value = seasonName;
        worksheet.Cell(rowNumber, 2).Value = plan.SeasonCategoryCode;
        worksheet.Cell(rowNumber, 3).Value = plan.TrainingTypeName;
        worksheet.Cell(rowNumber, 4).Value = plan.TrainingPhaseName;
        worksheet.Cell(rowNumber, 5).Value = plan.LocationName;
        worksheet.Cell(rowNumber, 6).Value = plan.From.ToDateTime(TimeOnly.MinValue);
        worksheet.Cell(rowNumber, 7).Value = plan.To.ToDateTime(TimeOnly.MinValue);
        worksheet.Cell(rowNumber, 8).Value = plan.Title;
        worksheet.Cell(rowNumber, 9).Value = WeekDayNames.GetFullName(plan.DayOfWeek);
        worksheet.Cell(rowNumber, 10).Value = plan.TimeFrom.ToTimeSpan();
        worksheet.Cell(rowNumber, 11).Value = plan.TimeTo.ToTimeSpan();
        if (plan.DurationMinutes.HasValue)
            worksheet.Cell(rowNumber, 12).Value = plan.DurationMinutes.Value;

        worksheet.Cell(rowNumber, 13).Value =
            TrainingPlanCoachPresentation.Format(plan.CoachAssignments);
    }

    private static void WriteCoverage(
        IXLWorksheet worksheet,
        int rowNumber,
        TrainingRequirementCoverageRow coverage)
    {
        worksheet.Cell(rowNumber, 1).Value = coverage.SeasonName;
        worksheet.Cell(rowNumber, 2).Value = coverage.SeasonCategoryName;
        worksheet.Cell(rowNumber, 3).Value = coverage.TrainingTypeName;
        worksheet.Cell(rowNumber, 4).Value = coverage.CoachName;
        worksheet.Cell(rowNumber, 5).Value = coverage.CoachRoleName;
        worksheet.Cell(rowNumber, 6).Value = coverage.RequiredHours;
        worksheet.Cell(rowNumber, 7).Value = coverage.PlannedHours;
        worksheet.Cell(rowNumber, 8).Value = coverage.StatusText;
    }
}
