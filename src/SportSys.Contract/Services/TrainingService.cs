using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
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
            .ThenBy(training => training.SeasonCategoryName)
            .ThenBy(training => training.Id)
            .Select(training => new TrainingEditMemberDto
            {
                Id = training.Id,
                SeasonCategoryOrder = training.SeasonCategory.Order,
                SeasonCategoryName = training.SeasonCategoryName,
                TrainingTypeName = training.TrainingType.Name,
                Date = training.Date,
                TimeFrom = training.TimeFrom,
                TimeTo = training.TimeTo,
                Location = training.Location,
                Note = training.Note,
            })
            .ToListAsync(ct);

        if (members.Count == 0)
            return null;

        var selected = members.First(member => member.Id == id);

        return new TrainingEditContextDto
        {
            Input = new TrainingEditDto
            {
                Id = selected.Id,
                OriginalVersion = CreateVersion(target.GroupId, members),
                Date = selected.Date,
                TimeFrom = selected.TimeFrom,
                TimeTo = selected.TimeTo,
                Location = selected.Location,
                Note = selected.Note,
            },
            Members = members,
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
                SeasonCategoryName = training.SeasonCategoryName,
                TrainingTypeName = training.TrainingType.Name,
                Date = training.Date,
                TimeFrom = training.TimeFrom,
                TimeTo = training.TimeTo,
                Location = training.Location,
                Note = training.Note,
            })
            .ToList();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(dto.OriginalVersion),
                Encoding.UTF8.GetBytes(CreateVersion(groupId, currentMembers))))
        {
            return TrainingUpdateResult.Conflict;
        }

        if (!HaveConsistentEditableValues(trainings))
            return TrainingUpdateResult.GroupInconsistent;

        foreach (var training in trainings)
        {
            training.Date = dto.Date;
            training.TimeFrom = dto.TimeFrom;
            training.TimeTo = dto.TimeTo;
            training.Location = dto.Location;
            training.Note = dto.Note ?? string.Empty;
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return TrainingUpdateResult.Success;
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
            string.Equals(member.Location, first.Location, StringComparison.Ordinal) &&
            string.Equals(member.Note, first.Note, StringComparison.Ordinal));
    }

    private static bool HaveConsistentEditableValues(IReadOnlyList<Training> trainings)
    {
        var first = trainings[0];
        return trainings.All(training =>
            training.Date == first.Date &&
            training.TimeFrom == first.TimeFrom &&
            training.TimeTo == first.TimeTo &&
            string.Equals(training.Location, first.Location, StringComparison.Ordinal) &&
            string.Equals(training.Note, first.Note, StringComparison.Ordinal));
    }

    private static bool IsValid(TrainingEditDto dto)
        => dto.Id > 0 &&
           dto.TimeFrom < dto.TimeTo &&
           !string.IsNullOrWhiteSpace(dto.Location) &&
           dto.Location.Length <= 100 &&
           (dto.Note?.Length ?? 0) <= 50 &&
           !string.IsNullOrEmpty(dto.OriginalVersion);

    private static string CreateVersion(
        Guid? groupId,
        IReadOnlyList<TrainingEditMemberDto> members)
    {
        var snapshot = new TrainingVersionSnapshot(
            groupId,
            members
                .OrderBy(member => member.Id)
                .Select(member => new TrainingMemberVersion(
                    member.Id,
                    member.Date,
                    member.TimeFrom,
                    member.TimeTo,
                    member.Location,
                    member.Note))
                .ToList());

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot)));
        return Convert.ToHexString(bytes);
    }

    private sealed record TrainingTarget(Guid? GroupId);
    private sealed record TrainingVersionSnapshot(
        Guid? GroupId,
        IReadOnlyList<TrainingMemberVersion> Members);
    private sealed record TrainingMemberVersion(
        int Id,
        DateOnly Date,
        TimeOnly TimeFrom,
        TimeOnly TimeTo,
        string Location,
        string Note);
}
