using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SportSys.Contract.Services;

public class TrainingPlanService
{
    private readonly SportSysDbContext _db;

    public TrainingPlanService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<TrainingPlanEditContextDto?> GetEditAsync(
        int id,
        CancellationToken ct = default)
    {
        var target = await _db.TrainingPlans
            .AsNoTracking()
            .Where(plan => plan.Id == id)
            .Select(plan => new TrainingPlanTarget(
                plan.GroupMembership == null
                    ? null
                    : plan.GroupMembership.GroupId))
            .FirstOrDefaultAsync(ct);

        if (target is null)
            return null;

        var members = await CreateMembersQuery(id, target.GroupId)
            .AsNoTracking()
            .OrderBy(plan => plan.SeasonCategory.Order)
            .ThenBy(plan => plan.SeasonCategoryName)
            .ThenBy(plan => plan.Id)
            .Select(plan => new TrainingPlanEditMemberDto
            {
                Id = plan.Id,
                SeasonCategoryOrder = plan.SeasonCategory.Order,
                SeasonCategoryName = plan.SeasonCategoryName,
                TrainingTypeName = plan.TrainingType.Name,
                From = plan.From,
                To = plan.To,
                DayName = plan.DayName,
                TimeFrom = plan.TimeFrom,
                TimeTo = plan.TimeTo,
                Location = plan.Location,
            })
            .ToListAsync(ct);

        if (members.Count == 0)
            return null;

        var selected = members.First(member => member.Id == id);

        return new TrainingPlanEditContextDto
        {
            Input = new TrainingPlanEditDto
            {
                Id = selected.Id,
                OriginalVersion = CreateVersion(target.GroupId, members),
                From = selected.From,
                To = selected.To,
                DayName = selected.DayName,
                TimeFrom = selected.TimeFrom,
                TimeTo = selected.TimeTo,
                Location = selected.Location,
            },
            Members = members,
            IsGrouped = target.GroupId.HasValue,
            CanEdit = HaveConsistentEditableValues(members),
        };
    }

    public async Task<TrainingPlanUpdateResult> UpdateAsync(
        TrainingPlanEditDto dto,
        CancellationToken ct = default)
    {
        if (!IsValid(dto))
            return TrainingPlanUpdateResult.InvalidInput;

        try
        {
            return await UpdateCoreAsync(dto, ct);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is SqlException { Number: 1205 })
        {
            return TrainingPlanUpdateResult.Conflict;
        }
        catch (SqlException ex) when (ex.Number == 1205)
        {
            return TrainingPlanUpdateResult.Conflict;
        }
    }

    private async Task<TrainingPlanUpdateResult> UpdateCoreAsync(
        TrainingPlanEditDto dto,
        CancellationToken ct)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        var target = await _db.TrainingPlans
            .Include(plan => plan.GroupMembership)
            .FirstOrDefaultAsync(plan => plan.Id == dto.Id, ct);

        if (target is null)
            return TrainingPlanUpdateResult.NotFound;

        var groupId = target.GroupMembership?.GroupId;
        var plans = await CreateMembersQuery(dto.Id, groupId)
            .Include(plan => plan.SeasonCategory)
            .Include(plan => plan.TrainingType)
            .OrderBy(plan => plan.Id)
            .ToListAsync(ct);

        var currentMembers = plans
            .Select(plan => new TrainingPlanEditMemberDto
            {
                Id = plan.Id,
                SeasonCategoryOrder = plan.SeasonCategory.Order,
                SeasonCategoryName = plan.SeasonCategoryName,
                TrainingTypeName = plan.TrainingType.Name,
                From = plan.From,
                To = plan.To,
                DayName = plan.DayName,
                TimeFrom = plan.TimeFrom,
                TimeTo = plan.TimeTo,
                Location = plan.Location,
            })
            .ToList();

        if (!CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(dto.OriginalVersion),
                Encoding.UTF8.GetBytes(CreateVersion(groupId, currentMembers))))
        {
            return TrainingPlanUpdateResult.Conflict;
        }

        if (!HaveConsistentEditableValues(currentMembers))
            return TrainingPlanUpdateResult.GroupInconsistent;

        foreach (var plan in plans)
        {
            plan.From = dto.From;
            plan.To = dto.To;
            plan.DayName = dto.DayName;
            plan.TimeFrom = dto.TimeFrom;
            plan.TimeTo = dto.TimeTo;
            plan.Location = dto.Location;
        }

        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return TrainingPlanUpdateResult.Success;
    }

    private IQueryable<TrainingPlan> CreateMembersQuery(int id, Guid? groupId)
        => groupId.HasValue
            ? _db.TrainingPlans.Where(plan =>
                plan.GroupMembership != null &&
                plan.GroupMembership.GroupId == groupId.Value)
            : _db.TrainingPlans.Where(plan => plan.Id == id);

    internal static bool HaveConsistentEditableValues(
        IReadOnlyList<TrainingPlanEditMemberDto> members)
    {
        var first = members[0];
        return members.All(member =>
            member.From == first.From &&
            member.To == first.To &&
            string.Equals(member.DayName, first.DayName, StringComparison.Ordinal) &&
            member.TimeFrom == first.TimeFrom &&
            member.TimeTo == first.TimeTo &&
            string.Equals(member.Location, first.Location, StringComparison.Ordinal));
    }

    private static bool IsValid(TrainingPlanEditDto dto)
        => dto.Id > 0 &&
           dto.From <= dto.To &&
           TrainingPlanEditDto.IsValidDayName(dto.DayName) &&
           dto.TimeFrom < dto.TimeTo &&
           !string.IsNullOrWhiteSpace(dto.Location) &&
           dto.Location.Length <= 100 &&
           !string.IsNullOrEmpty(dto.OriginalVersion);

    internal static string CreateVersion(
        Guid? groupId,
        IReadOnlyList<TrainingPlanEditMemberDto> members)
    {
        var snapshot = new TrainingPlanVersionSnapshot(
            groupId,
            members
                .OrderBy(member => member.Id)
                .Select(member => new TrainingPlanMemberVersion(
                    member.Id,
                    member.From,
                    member.To,
                    member.DayName,
                    member.TimeFrom,
                    member.TimeTo,
                    member.Location))
                .ToList());

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot)));
        return Convert.ToHexString(bytes);
    }

    private sealed record TrainingPlanTarget(Guid? GroupId);
    private sealed record TrainingPlanVersionSnapshot(
        Guid? GroupId,
        IReadOnlyList<TrainingPlanMemberVersion> Members);
    private sealed record TrainingPlanMemberVersion(
        int Id,
        DateOnly From,
        DateOnly To,
        string DayName,
        TimeOnly TimeFrom,
        TimeOnly TimeTo,
        string Location);
}
