namespace SportSys.Contract.Models.hr;

public sealed class CoachAttendanceImportResult
{
    public int AttendanceId { get; set; }
    public int SeasonId { get; set; }
    public int TrainingCount { get; set; }
    public int MatchCount { get; set; }
    public int InsertedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int UnchangedCount { get; set; }
}

internal enum CoachAttendanceEventKind
{
    Training,
    Match,
}

internal sealed record CoachAttendanceImportRow(
    int RowNumber,
    DateOnly Date,
    TimeOnly TimeFrom,
    TimeOnly? TimeTo,
    string Name,
    CoachAttendanceEventKind EventKind,
    string RosterName,
    string ParticipationName,
    string Note);

internal sealed record CoachAttendanceParsedFile(
    string FileName,
    string ContentType,
    IReadOnlyList<CoachAttendanceImportRow> Rows);

internal sealed record CoachAttendanceCategorySnapshot(
    string Code,
    string Name);

internal sealed record CoachAttendanceTrainingSnapshot(
    int Id,
    DateOnly Date,
    TimeOnly TimeFrom,
    TimeOnly TimeTo,
    string SeasonCategoryCode,
    int TrainingStateId);

internal sealed record CoachAttendanceMatchSnapshot(
    int Id,
    DateOnly Date,
    TimeOnly TimeFrom,
    string SeasonCategoryCode,
    string HomeTeamName,
    string AwayTeamName,
    int? MatchStateId);

internal sealed record CoachAttendanceParticipationSnapshot(
    int Id,
    string Name);

internal sealed record CoachAttendanceResolvedRow(
    int RowNumber,
    CoachAttendanceEventKind EventKind,
    int EventId,
    int ParticipationTypeId,
    string Note);
