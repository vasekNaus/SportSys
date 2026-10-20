using SportSys.Contract.Models.hr;
using SportSys.Database.Enums;

namespace SportSys.Contract.Services;

internal static class CoachAttendanceImportPlanner
{
    private const int MaxErrors = 50;

    internal static IReadOnlyList<CoachAttendanceResolvedRow> Resolve(
        IReadOnlyList<CoachAttendanceImportRow> rows,
        DateOnly seasonFrom,
        DateOnly seasonTo,
        IReadOnlyList<CoachAttendanceCategorySnapshot> categories,
        IReadOnlyList<CoachAttendanceTrainingSnapshot> trainings,
        IReadOnlyList<CoachAttendanceMatchSnapshot> matches,
        IReadOnlyList<CoachAttendanceParticipationSnapshot> participationTypes)
    {
        var resolved = new List<CoachAttendanceResolvedRow>(rows.Count);
        var errors = new List<string>();
        var resolvedEvents = new Dictionary<(CoachAttendanceEventKind Kind, int Id), int>();

        foreach (var row in rows)
        {
            if (errors.Count >= MaxErrors)
                break;

            if (row.Date < seasonFrom || row.Date > seasonTo)
            {
                errors.Add($"Řádek {row.RowNumber}: datum neleží v intervalu vybrané sezony.");
                continue;
            }

            var categoryMatches = categories
                .Where(category =>
                    EqualsNormalized(category.Name, row.RosterName)
                    || EqualsNormalized(category.Code, row.RosterName))
                .Take(2)
                .ToList();
            if (categoryMatches.Count != 1)
            {
                errors.Add(categoryMatches.Count == 0
                    ? $"Řádek {row.RowNumber}: soupiska '{row.RosterName}' nebyla ve vybrané sezoně nalezena."
                    : $"Řádek {row.RowNumber}: soupiska '{row.RosterName}' je ve vybrané sezoně nejednoznačná.");
                continue;
            }

            var participationMatches = participationTypes
                .Where(type => EqualsNormalized(type.Name, row.ParticipationName))
                .Take(2)
                .ToList();
            if (participationMatches.Count != 1)
            {
                errors.Add(participationMatches.Count == 0
                    ? $"Řádek {row.RowNumber}: stav účasti '{row.ParticipationName}' nebyl nalezen."
                    : $"Řádek {row.RowNumber}: stav účasti '{row.ParticipationName}' je nejednoznačný.");
                continue;
            }

            var eventId = row.EventKind switch
            {
                CoachAttendanceEventKind.Training => ResolveTraining(
                    row,
                    categoryMatches[0].Code,
                    trainings,
                    errors),
                CoachAttendanceEventKind.Match => ResolveMatch(
                    row,
                    categoryMatches[0].Code,
                    matches,
                    errors),
                _ => null,
            };
            if (!eventId.HasValue)
                continue;

            var eventKey = (row.EventKind, eventId.Value);
            if (resolvedEvents.TryGetValue(eventKey, out var previousRow))
            {
                errors.Add(
                    $"Řádky {previousRow} a {row.RowNumber}: stejná událost je v souboru uvedena vícekrát.");
                continue;
            }

            resolvedEvents.Add(eventKey, row.RowNumber);
            resolved.Add(new CoachAttendanceResolvedRow(
                row.RowNumber,
                row.EventKind,
                eventId.Value,
                participationMatches[0].Id,
                row.Note));
        }

        if (errors.Count > 0)
            throw new CoachValidationException(errors.Take(MaxErrors));

        return resolved;
    }

    private static int? ResolveTraining(
        CoachAttendanceImportRow row,
        string categoryCode,
        IReadOnlyList<CoachAttendanceTrainingSnapshot> trainings,
        ICollection<string> errors)
    {
        var candidates = trainings
            .Where(training =>
                training.Date == row.Date
                && training.SeasonCategoryCode.Equals(categoryCode, StringComparison.OrdinalIgnoreCase)
                && training.TimeFrom == row.TimeFrom
                && training.TimeTo == row.TimeTo
                && training.TrainingStateId is not (int)ETrainingState.Cancelled
                    and not (int)ETrainingState.DateChanged
                    and not (int)ETrainingState.ArenaFault)
            .Take(2)
            .ToList();

        if (candidates.Count == 1)
            return candidates[0].Id;

        errors.Add(candidates.Count == 0
            ? $"Řádek {row.RowNumber}: odpovídající trénink nebyl nalezen."
            : $"Řádek {row.RowNumber}: odpovídající trénink je nejednoznačný.");
        return null;
    }

    private static int? ResolveMatch(
        CoachAttendanceImportRow row,
        string categoryCode,
        IReadOnlyList<CoachAttendanceMatchSnapshot> matches,
        ICollection<string> errors)
    {
        var candidates = matches
            .Where(match =>
                match.Date == row.Date
                && match.SeasonCategoryCode.Equals(categoryCode, StringComparison.OrdinalIgnoreCase)
                && match.MatchStateId != (int)EMatchState.Cancelled
                && EqualsNormalized($"{match.HomeTeamName} - {match.AwayTeamName}", row.Name))
            .ToList();

        if (candidates.Count == 1)
            return candidates[0].Id;

        if (candidates.Count > 1)
        {
            var timeMatches = candidates
                .Where(match => match.TimeFrom == row.TimeFrom)
                .Take(2)
                .ToList();
            if (timeMatches.Count == 1)
                return timeMatches[0].Id;
        }

        //HACK
        //errors.Add(candidates.Count == 0
        //    ? $"Řádek {row.RowNumber}: odpovídající zápas nebyl nalezen."
        //    : $"Řádek {row.RowNumber}: odpovídající zápas je nejednoznačný.");
        return null;
    }

    private static bool EqualsNormalized(string left, string right) =>
        Normalize(left).Equals(Normalize(right), StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value) =>
        string.Join(' ', value.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
