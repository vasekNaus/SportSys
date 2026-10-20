using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Enums;
using SportSys.Database.Models.sport;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SportSys.Contract.Services;

public class TrainingService
{
    private readonly SportSysDbContext _db;

    public TrainingService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<TrainingEditContextDto?> GetEditAsync(
        int id,
        CancellationToken ct = default)
    {
        var target = await _db.Training
            .AsNoTracking()
            .Where(training => training.Id == id)
            .Select(training => new TrainingTarget(
                training.GroupMembership == null
                    ? null
                    : training.GroupMembership.GroupId))
            .FirstOrDefaultAsync(ct);

        if (target is null)
            return null;

        var members = await CreateMembersQuery(id, target.GroupId)
            .AsNoTracking()
            .OrderBy(training => training.SeasonCategory.Order)
            .ThenBy(training => training.SeasonCategoryCode)
            .ThenBy(training => training.Id)
            .Select(training => new TrainingEditMemberDto
            {
                Id = training.Id,
                SeasonCategoryOrder = training.SeasonCategory.Order,
                SeasonCategoryCode = training.SeasonCategoryCode,
                TrainingTypeName = training.TrainingType.Name,
                Date = training.Date,
                TimeFrom = training.TimeFrom,
                TimeTo = training.TimeTo,
                LocationId = training.LocationId,
                LocationName = training.Location.Name,
                Note = training.Note,
            })
            .ToListAsync(ct);

        if (members.Count == 0)
            return null;

        var selected = members.First(member => member.Id == id);
        var memberIds = members.Select(member => member.Id).ToList();
        var assignmentRows = await LoadCoachAssignmentsAsync(memberIds, ct);
        var assignments = assignmentRows
            .Select(row => new TrainingEditCoachAssignmentDto
            {
                CoachId = row.CoachId,
                TrainingId = row.TrainingId,
                ParticipationTypeId = row.ParticipationTypeId,
                CoachDisplayName = row.CoachDisplayName,
                SeasonCategoryCode = row.SeasonCategoryCode,
                ParticipationTypeName = ((EParticipationType)row.ParticipationTypeId).GetDisplayName(),
                Note = row.Note,
            })
            .ToList();
        var availableCoaches = await _db.Coaches
            .AsNoTracking()
            .OrderBy(coach => coach.DisplayName ?? coach.UserName ?? coach.Email)
            .ThenBy(coach => coach.PersonalNumber)
            .Select(coach => new TrainingCoachSelectItemDto
            {
                Id = coach.Id,
                DisplayName = coach.DisplayName ?? coach.UserName ?? coach.Email ?? coach.Id.ToString(),
            })
            .ToListAsync(ct);

        return new TrainingEditContextDto
        {
            Input = new TrainingEditDto
            {
                Id = selected.Id,
                OriginalVersion = CreateVersion(target.GroupId, members, assignments),
                Date = selected.Date,
                TimeFrom = selected.TimeFrom,
                TimeTo = selected.TimeTo,
                LocationId = selected.LocationId,
                Note = selected.Note,
                CoachAssignments = assignments
                    .Select(assignment => new TrainingCoachAssignmentInputDto
                    {
                        ExistingCoachId = assignment.CoachId,
                        ExistingTrainingId = assignment.TrainingId,
                        ExistingParticipationTypeId = assignment.ParticipationTypeId,
                        ParticipationTypeId = assignment.ParticipationTypeId,
                        Note = assignment.Note,
                    })
                    .ToList(),
            },
            Members = members,
            CoachAssignments = assignments,
            AvailableCoaches = availableCoaches,
            AvailableCategories = members
                .Select(member => new TrainingCategorySelectItemDto
                {
                    TrainingId = member.Id,
                    CategoryCode = member.SeasonCategoryCode,
                })
                .ToList(),
            ParticipationTypes = Enum.GetValues<EParticipationType>()
                .Select(participationType => new TrainingParticipationTypeSelectItemDto
                {
                    Id = (int)participationType,
                    DisplayName = participationType.GetDisplayName(),
                    IsDefault = participationType == EParticipationType.Absent,
                })
                .ToList(),
            IsGrouped = target.GroupId.HasValue,
            CanEdit = HaveConsistentEditableValues(members),
        };
    }

    public async Task<TrainingUpdateResult> UpdateAsync(
        TrainingEditDto dto,
        CancellationToken ct = default)
    {
        if (!IsValid(dto))
            return TrainingUpdateResult.InvalidInput;

        try
        {
            return await UpdateCoreAsync(dto, ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: 1205 })
        {
            return TrainingUpdateResult.Conflict;
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            return TrainingUpdateResult.Conflict;
        }
    }

    private async Task<TrainingUpdateResult> UpdateCoreAsync(
        TrainingEditDto dto,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var target = await _db.Training
            .Include(training => training.GroupMembership)
            .FirstOrDefaultAsync(training => training.Id == dto.Id, ct);

        if (target is null)
            return TrainingUpdateResult.NotFound;

        var groupId = target.GroupMembership?.GroupId;
        var trainings = await CreateMembersQuery(dto.Id, groupId)
            .Include(training => training.SeasonCategory)
            .Include(training => training.TrainingType)
            .OrderBy(training => training.Id)
            .ToListAsync(ct);

        var currentMembers = trainings
            .Select(training => new TrainingEditMemberDto
            {
                Id = training.Id,
                SeasonCategoryOrder = training.SeasonCategory.Order,
                SeasonCategoryCode = training.SeasonCategoryCode,
                TrainingTypeName = training.TrainingType.Name,
                Date = training.Date,
                TimeFrom = training.TimeFrom,
                TimeTo = training.TimeTo,
                LocationId = training.LocationId,
                LocationName = training.Location.Name,
                Note = training.Note,
            })
            .ToList();

        var memberIds = trainings.Select(training => training.Id).ToList();
        var existingCoachAssignments = await _db.CoachTrainings
            .Where(assignment => memberIds.Contains(assignment.TrainingId))
            .ToListAsync(ct);
        var currentAssignmentRows = await LoadCoachAssignmentsAsync(memberIds, ct);
        var currentAssignments = currentAssignmentRows
            .Select(row => new TrainingEditCoachAssignmentDto
            {
                CoachId = row.CoachId,
                TrainingId = row.TrainingId,
                ParticipationTypeId = row.ParticipationTypeId,
                CoachDisplayName = row.CoachDisplayName,
                SeasonCategoryCode = row.SeasonCategoryCode,
                ParticipationTypeName = ((EParticipationType)row.ParticipationTypeId).GetDisplayName(),
                Note = row.Note,
            })
            .ToList();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(dto.OriginalVersion),
                Encoding.UTF8.GetBytes(CreateVersion(groupId, currentMembers, currentAssignments))))
        {
            return TrainingUpdateResult.Conflict;
        }

        if (!HaveConsistentEditableValues(trainings))
            return TrainingUpdateResult.GroupInconsistent;

        var retainsSelectedInactiveLocation = trainings.All(
            training => training.LocationId == dto.LocationId);
        var locationAvailable = await _db.SportLocations.AnyAsync(
            location => location.Id == dto.LocationId &&
                (location.IsActive || retainsSelectedInactiveLocation),
            ct);
        if (!locationAvailable)
            return TrainingUpdateResult.LocationUnavailable;

        var availableCoachIds = await _db.Coaches
            .Select(coach => coach.Id)
            .ToListAsync(ct);
        var assignmentValidation = ValidateCoachAssignments(
            dto.CoachAssignments,
            existingCoachAssignments
                .Select(assignment => new CoachTrainingAssignmentKey(
                    assignment.CoachId,
                    assignment.TrainingId,
                    assignment.ParticipationTypeId))
                .ToList(),
            memberIds.ToHashSet(),
            availableCoachIds.ToHashSet());

        if (assignmentValidation.Result != TrainingUpdateResult.Success)
            return assignmentValidation.Result;

        foreach (var training in trainings)
        {
            training.Date = dto.Date;
            training.TimeFrom = dto.TimeFrom;
            training.TimeTo = dto.TimeTo;
            training.LocationId = dto.LocationId;
            training.Note = dto.Note ?? string.Empty;
        }

        var existingAssignmentsByKey = existingCoachAssignments.ToDictionary(
            assignment => new CoachTrainingAssignmentKey(
                assignment.CoachId,
                assignment.TrainingId,
                assignment.ParticipationTypeId));
        foreach (var assignment in assignmentValidation.Assignments)
        {
            if (assignment.ExistingParticipationTypeId.HasValue)
            {
                var existingKey = new CoachTrainingAssignmentKey(
                    assignment.CoachId,
                    assignment.TrainingId,
                    assignment.ExistingParticipationTypeId.Value);
                var existing = existingAssignmentsByKey[existingKey];
                if (existing.ParticipationTypeId == assignment.ParticipationTypeId)
                {
                    existing.Note = assignment.Note;
                }
                else
                {
                    _db.CoachTrainings.Remove(existing);
                    _db.CoachTrainings.Add(new CoachTraining
                    {
                        CoachId = assignment.CoachId,
                        TrainingId = assignment.TrainingId,
                        ParticipationTypeId = assignment.ParticipationTypeId,
                        Note = assignment.Note,
                    });
                }
            }
            else
            {
                _db.CoachTrainings.Add(new CoachTraining
                {
                    CoachId = assignment.CoachId,
                    TrainingId = assignment.TrainingId,
                    ParticipationTypeId = assignment.ParticipationTypeId,
                    Note = assignment.Note,
                });
            }
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return TrainingUpdateResult.Success;
    }

    private Task<List<CoachAssignmentRow>> LoadCoachAssignmentsAsync(
        IReadOnlyCollection<int> memberIds,
        CancellationToken ct)
        => _db.CoachTrainings
            .AsNoTracking()
            .Where(assignment => memberIds.Contains(assignment.TrainingId))
            .OrderBy(assignment => assignment.Training.SeasonCategory.Order)
            .ThenBy(assignment => assignment.Training.SeasonCategoryCode)
            .ThenBy(assignment =>
                assignment.Coach.DisplayName ?? assignment.Coach.UserName ?? assignment.Coach.Email)
            .ThenBy(assignment => assignment.CoachId)
            .ThenBy(assignment => assignment.TrainingId)
            .ThenBy(assignment => assignment.ParticipationTypeId)
            .Select(assignment => new CoachAssignmentRow(
                assignment.CoachId,
                assignment.TrainingId,
                assignment.ParticipationTypeId,
                assignment.Coach.DisplayName ??
                    assignment.Coach.UserName ??
                    assignment.Coach.Email ??
                    assignment.Coach.Id.ToString(),
                assignment.Training.SeasonCategoryCode,
                assignment.Note))
            .ToListAsync(ct);

    internal static CoachTrainingAssignmentValidation ValidateCoachAssignments(
        IReadOnlyCollection<TrainingCoachAssignmentInputDto>? requestedAssignments,
        IReadOnlyCollection<CoachTrainingAssignmentKey> existingAssignmentKeys,
        IReadOnlySet<int> memberIds,
        IReadOnlySet<int> availableCoachIds)
    {
        if (requestedAssignments is null)
        {
            return new CoachTrainingAssignmentValidation(
                TrainingUpdateResult.InvalidInput,
                []);
        }

        var existingKeys = existingAssignmentKeys.ToHashSet();
        var submittedExistingKeys = new HashSet<CoachTrainingAssignmentKey>();
        var submittedCoachIds = new HashSet<int>();
        var updates = new List<CoachTrainingAssignmentUpdate>(requestedAssignments.Count);

        foreach (var assignment in requestedAssignments)
        {
            if (assignment is null ||
                assignment.ParticipationTypeId is null ||
                !Enum.IsDefined((EParticipationType)assignment.ParticipationTypeId.Value) ||
                (assignment.Note?.Length ?? 0) > 50)
            {
                return new CoachTrainingAssignmentValidation(
                    TrainingUpdateResult.InvalidInput,
                    []);
            }

            var hasExistingKey =
                assignment.ExistingCoachId.HasValue ||
                assignment.ExistingTrainingId.HasValue ||
                assignment.ExistingParticipationTypeId.HasValue;

            int coachId;
            int trainingId;
            int? existingParticipationTypeId;
            if (hasExistingKey)
            {
                if (assignment.ExistingCoachId is null or <= 0 ||
                    assignment.ExistingTrainingId is null or <= 0 ||
                    assignment.ExistingParticipationTypeId is null or <= 0 ||
                    assignment.CoachId.HasValue ||
                    assignment.TrainingId.HasValue)
                {
                    return new CoachTrainingAssignmentValidation(
                        TrainingUpdateResult.InvalidInput,
                        []);
                }

                var existingKey = new CoachTrainingAssignmentKey(
                    assignment.ExistingCoachId.Value,
                    assignment.ExistingTrainingId.Value,
                    assignment.ExistingParticipationTypeId.Value);
                if (!existingKeys.Contains(existingKey) ||
                    !submittedExistingKeys.Add(existingKey))
                {
                    return new CoachTrainingAssignmentValidation(
                        TrainingUpdateResult.InvalidInput,
                        []);
                }

                coachId = existingKey.CoachId;
                trainingId = existingKey.TrainingId;
                existingParticipationTypeId = existingKey.ParticipationTypeId;
            }
            else
            {
                if (assignment.CoachId is null or <= 0 ||
                    assignment.TrainingId is null or <= 0)
                {
                    return new CoachTrainingAssignmentValidation(
                        TrainingUpdateResult.InvalidInput,
                        []);
                }

                coachId = assignment.CoachId.Value;
                trainingId = assignment.TrainingId.Value;
                existingParticipationTypeId = null;
            }

            if (!memberIds.Contains(trainingId) ||
                !availableCoachIds.Contains(coachId))
            {
                return new CoachTrainingAssignmentValidation(
                    TrainingUpdateResult.InvalidInput,
                    []);
            }

            if (!submittedCoachIds.Add(coachId))
            {
                return new CoachTrainingAssignmentValidation(
                    TrainingUpdateResult.DuplicateCoachAssignment,
                    []);
            }

            updates.Add(new CoachTrainingAssignmentUpdate(
                coachId,
                trainingId,
                assignment.ParticipationTypeId.Value,
                assignment.Note ?? string.Empty,
                existingParticipationTypeId));
        }

        if (!existingKeys.SetEquals(submittedExistingKeys))
        {
            return new CoachTrainingAssignmentValidation(
                TrainingUpdateResult.CoachAssignmentRemovalNotAllowed,
                []);
        }

        return new CoachTrainingAssignmentValidation(
            TrainingUpdateResult.Success,
            updates);
    }

    private IQueryable<Training> CreateMembersQuery(int id, Guid? groupId)
        => groupId.HasValue
            ? _db.Training.Where(training =>
                training.GroupMembership != null &&
                training.GroupMembership.GroupId == groupId.Value)
            : _db.Training.Where(training => training.Id == id);

    private static bool HaveConsistentEditableValues(
        IReadOnlyList<TrainingEditMemberDto> members)
    {
        var first = members[0];
        return members.All(member =>
            member.Date == first.Date &&
            member.TimeFrom == first.TimeFrom &&
            member.TimeTo == first.TimeTo &&
            member.LocationId == first.LocationId &&
            string.Equals(member.Note, first.Note, StringComparison.Ordinal));
    }

    private static bool HaveConsistentEditableValues(IReadOnlyList<Training> trainings)
    {
        var first = trainings[0];
        return trainings.All(training =>
            training.Date == first.Date &&
            training.TimeFrom == first.TimeFrom &&
            training.TimeTo == first.TimeTo &&
            training.LocationId == first.LocationId &&
            string.Equals(training.Note, first.Note, StringComparison.Ordinal));
    }

    private static bool IsValid(TrainingEditDto dto)
        => dto.Id > 0 &&
           dto.TimeFrom < dto.TimeTo &&
           dto.LocationId > 0 &&
           (dto.Note?.Length ?? 0) <= 50 &&
           dto.CoachAssignments is not null &&
           !string.IsNullOrEmpty(dto.OriginalVersion);

    internal static string CreateVersion(
        Guid? groupId,
        IReadOnlyList<TrainingEditMemberDto> members,
        IReadOnlyList<TrainingEditCoachAssignmentDto> coachAssignments)
    {
        var snapshot = new TrainingVersionSnapshot(
            groupId,
            members
                .OrderBy(member => member.Id)
                .Select(member => new TrainingMemberVersion(
                    member.Id,
                    member.SeasonCategoryCode,
                    member.Date,
                    member.TimeFrom,
                    member.TimeTo,
                    member.LocationId,
                    member.Note))
                .ToList(),
            coachAssignments
                .OrderBy(assignment => assignment.CoachId)
                .ThenBy(assignment => assignment.TrainingId)
                .ThenBy(assignment => assignment.ParticipationTypeId)
                .Select(assignment => new TrainingCoachAssignmentVersion(
                    assignment.CoachId,
                    assignment.TrainingId,
                    assignment.ParticipationTypeId,
                    assignment.Note))
                .ToList());

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot)));
        return Convert.ToHexString(bytes);
    }

    private sealed record TrainingTarget(Guid? GroupId);
    private sealed record CoachAssignmentRow(
        int CoachId,
        int TrainingId,
        int ParticipationTypeId,
        string CoachDisplayName,
        string SeasonCategoryCode,
        string Note);
    private sealed record TrainingVersionSnapshot(
        Guid? GroupId,
        IReadOnlyList<TrainingMemberVersion> Members,
        IReadOnlyList<TrainingCoachAssignmentVersion> CoachAssignments);
    private sealed record TrainingMemberVersion(
        int Id,
        string SeasonCategoryCode,
        DateOnly Date,
        TimeOnly TimeFrom,
        TimeOnly TimeTo,
        int LocationId,
        string Note);
    private sealed record TrainingCoachAssignmentVersion(
        int CoachId,
        int TrainingId,
        int ParticipationTypeId,
        string Note);
}
