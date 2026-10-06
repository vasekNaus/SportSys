using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using SportSys.Contract.Models;
using SportSys.Contract.Models.hr;
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

    var memberRows = await CreateMembersQuery(id, target.GroupId)
        .AsNoTracking()
        .OrderBy(plan => plan.SeasonCategory.Order)
        .ThenBy(plan => plan.SeasonCategoryName)
        .ThenBy(plan => plan.Id)
        .Select(plan => new MemberRow(
            plan.Id,
            plan.SeasonCategory.Order,
            plan.SeasonCategoryName,
            plan.TrainingType.Name,
            plan.From,
            plan.To,
            plan.DayName,
            plan.TimeFrom,
            plan.TimeTo,
            plan.LocationId))
        .ToListAsync(ct);

    if (memberRows.Count == 0)
      return null;

    var memberIds = memberRows.Select(row => row.Id).ToList();
    var coachIdsByMember = await LoadCoachIdsByMemberAsync(memberIds, ct);

    var members = memberRows
        .Select(row => new TrainingPlanEditMemberDto
        {
          Id = row.Id,
          SeasonCategoryOrder = row.SeasonCategoryOrder,
          SeasonCategoryName = row.SeasonCategoryName,
          TrainingTypeName = row.TrainingTypeName,
          From = row.From,
          To = row.To,
          DayName = row.DayName,
          TimeFrom = row.TimeFrom,
          TimeTo = row.TimeTo,
          LocationId = row.LocationId,
          CoachIds = coachIdsByMember.TryGetValue(row.Id, out var coachIds)
                ? coachIds
                : [],
        })
        .ToList();

    var selected = members.First(member => member.Id == id);
    var availableCoaches = await GetAvailableCoachesAsync(ct);

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
        LocationId = selected.LocationId,
        SelectedCoachIds = selected.CoachIds.ToList(),
        MemberCoachAssignments = target.GroupId.HasValue
                ? members
                    .Select(member => new TrainingPlanMemberCoachInputDto
                    {
                      TrainingPlanId = member.Id,
                      CoachIds = member.CoachIds.ToList(),
                    })
                    .ToList()
                : [],
      },
      Members = members,
      AvailableCoaches = availableCoaches,
      IsGrouped = target.GroupId.HasValue,
      CanEdit = HaveConsistentEditableValues(members),
    };
  }

  private Task<List<CoachSelectItem>> GetAvailableCoachesAsync(CancellationToken ct)
      => _db.Coaches
          .AsNoTracking()
          .OrderBy(coach => coach.DisplayName ?? coach.UserName ?? coach.Email)
          .ThenBy(coach => coach.PersonalNumber)
          .Select(coach => new CoachSelectItem
          {
            Id = coach.Id,
            DisplayName = coach.DisplayName ?? coach.UserName ?? coach.Email ?? coach.Id.ToString(),
            Email = coach.Email,
            PersonalNumber = coach.PersonalNumber,
          })
          .ToListAsync(ct);

  private async Task<Dictionary<int, List<int>>> LoadCoachIdsByMemberAsync(
      IReadOnlyCollection<int> memberIds,
      CancellationToken ct)
  {
    var assignments = await _db.CoachTrainingPlans
        .AsNoTracking()
        .Where(assignment => memberIds.Contains(assignment.TrainingPlanId))
        .Select(assignment => new { assignment.TrainingPlanId, assignment.CoachId })
        .ToListAsync(ct);

    return assignments
        .GroupBy(assignment => assignment.TrainingPlanId)
        .ToDictionary(
            group => group.Key,
            group => group.Select(assignment => assignment.CoachId).Distinct().ToList());
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

    var memberIds = plans.Select(plan => plan.Id).ToList();
    var existingAssignments = await _db.CoachTrainingPlans
        .Where(assignment => memberIds.Contains(assignment.TrainingPlanId))
        .ToListAsync(ct);
    var assignmentsByPlan = existingAssignments.ToLookup(assignment => assignment.TrainingPlanId);

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
          LocationId = plan.LocationId,
          CoachIds = assignmentsByPlan[plan.Id]
                .Select(assignment => assignment.CoachId)
                .Distinct()
                .ToList(),
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

    var retainsSelectedInactiveLocation = currentMembers.All(
        member => member.LocationId == dto.LocationId);
    var locationAvailable = await _db.SportLocations.AnyAsync(
        location => location.Id == dto.LocationId &&
            (location.IsActive || retainsSelectedInactiveLocation),
        ct);
    if (!locationAvailable)
      return TrainingPlanUpdateResult.LocationUnavailable;

    var availableCoachIds = await _db.Coaches
        .Select(coach => coach.Id)
        .ToListAsync(ct);

    var requestedCoachIdsByPlan = BuildRequestedCoachIdsByPlan(
        dto,
        groupId,
        memberIds,
        availableCoachIds);

    //HACK dočasně umožníme přiřazení trenérů ke spojeným tréninkům
    //if (HasDuplicateCoachAcrossPlans(requestedCoachIdsByPlan))
    //    return TrainingPlanUpdateResult.DuplicateCoachAssignment;

    foreach (var plan in plans)
    {
      plan.From = dto.From;
      plan.To = dto.To;
      plan.DayName = dto.DayName;
      plan.TimeFrom = dto.TimeFrom;
      plan.TimeTo = dto.TimeTo;
      plan.LocationId = dto.LocationId;
    }

    foreach (var plan in plans)
    {
      var selectedCoachIds = requestedCoachIdsByPlan.TryGetValue(plan.Id, out var ids)
          ? ids
          : [];

      SynchronizeCoachAssignments(
          plan.Id,
          dto.From,
          dto.To,
          selectedCoachIds,
          assignmentsByPlan[plan.Id].ToList());
    }

    await _db.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
    return TrainingPlanUpdateResult.Success;
  }

  /// <summary>
  /// Sestaví normalizovaný výběr trenérů pro každý `TrainingPlanId` ve
  /// skupině. U spojeného plánu se vstup čte z
  /// <see cref="TrainingPlanEditDto.MemberCoachAssignments"/>, a to jen
  /// pro ID skutečně existujících členů (<paramref name="memberIds"/>) —
  /// položky pro cizí/neexistující ID z requestu se ignorují. U
  /// nespojeného plánu se použije <see cref="TrainingPlanEditDto.SelectedCoachIds"/>
  /// pro jediný `dto.Id`.
  /// </summary>
  internal static Dictionary<int, List<int>> BuildRequestedCoachIdsByPlan(
      TrainingPlanEditDto dto,
      Guid? groupId,
      IReadOnlyCollection<int> memberIds,
      IEnumerable<int> availableCoachIds)
  {
    var available = availableCoachIds.ToList();

    if (!groupId.HasValue)
    {
      return new Dictionary<int, List<int>>
      {
        [dto.Id] = NormalizeCoachIds(dto.SelectedCoachIds, available),
      };
    }

    var memberIdSet = memberIds.ToHashSet();
    var result = memberIds.ToDictionary(id => id, _ => new List<int>());

    foreach (var assignment in dto.MemberCoachAssignments)
    {
      if (!memberIdSet.Contains(assignment.TrainingPlanId))
        continue;

      result[assignment.TrainingPlanId] = NormalizeCoachIds(assignment.CoachIds, available);
    }

    return result;
  }

  internal static bool HasDuplicateCoachAcrossPlans(
      IReadOnlyDictionary<int, List<int>> coachIdsByPlan)
  {
    var seen = new HashSet<int>();
    foreach (var coachIds in coachIdsByPlan.Values)
    {
      foreach (var coachId in coachIds)
      {
        if (!seen.Add(coachId))
          return true;
      }
    }

    return false;
  }

  private void SynchronizeCoachAssignments(
      int trainingPlanId,
      DateOnly validFrom,
      DateOnly validTo,
      IReadOnlyCollection<int> selectedCoachIds,
      IReadOnlyCollection<CoachTrainingPlan> existingAssignments)
  {
    var diff = ComputeCoachAssignmentDiff(
        selectedCoachIds,
        existingAssignments.Select(assignment => assignment.CoachId).Distinct().ToList());

    var assignmentsByCoach = existingAssignments.ToLookup(assignment => assignment.CoachId);

    foreach (var coachId in diff.ToRemove)
      _db.CoachTrainingPlans.RemoveRange(assignmentsByCoach[coachId]);

    foreach (var coachId in diff.ToKeep)
    {
      var rows = assignmentsByCoach[coachId].ToList();
      var matching = rows.FirstOrDefault(
          assignment => assignment.ValidFrom == validFrom && assignment.ValidTo == validTo);

      _db.CoachTrainingPlans.RemoveRange(
          matching is null ? rows : rows.Where(assignment => assignment != matching));

      if (matching is null)
        _db.CoachTrainingPlans.Add(
            CreateCoachTrainingPlan(trainingPlanId, validFrom, validTo, coachId));
    }

    _db.CoachTrainingPlans.AddRange(
        diff.ToAdd.Select(coachId =>
            CreateCoachTrainingPlan(trainingPlanId, validFrom, validTo, coachId)));
  }

  private static CoachTrainingPlan CreateCoachTrainingPlan(
      int trainingPlanId, DateOnly validFrom, DateOnly validTo, int coachId)
      => new()
      {
        CoachId = coachId,
        TrainingPlanId = trainingPlanId,
        ValidFrom = validFrom,
        ValidTo = validTo,
      };

  internal static List<int> NormalizeCoachIds(
      IEnumerable<int> requestedCoachIds,
      IEnumerable<int> availableCoachIds)
  {
    var available = availableCoachIds.ToHashSet();
    return requestedCoachIds
        .Where(available.Contains)
        .Distinct()
        .ToList();
  }

  internal static CoachAssignmentDiff ComputeCoachAssignmentDiff(
      IReadOnlyCollection<int> selectedCoachIds,
      IReadOnlyCollection<int> existingCoachIds)
  {
    var selected = selectedCoachIds.ToHashSet();
    var existing = existingCoachIds.ToHashSet();

    return new CoachAssignmentDiff(
        ToAdd: selected.Where(id => !existing.Contains(id)).ToList(),
        ToKeep: selected.Where(existing.Contains).ToList(),
        ToRemove: existing.Where(id => !selected.Contains(id)).ToList());
  }

  internal sealed record CoachAssignmentDiff(
      List<int> ToAdd,
      List<int> ToKeep,
      List<int> ToRemove);

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
        member.LocationId == first.LocationId);
  }

  private static bool IsValid(TrainingPlanEditDto dto)
      => dto.Id > 0 &&
         dto.From <= dto.To &&
         TrainingPlanEditDto.IsValidDayName(dto.DayName) &&
         dto.TimeFrom < dto.TimeTo &&
         dto.LocationId > 0 &&
         dto.Title is not null &&
         dto.Title.Length <= 100 &&
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
                member.LocationId,
                member.CoachIds.Distinct().OrderBy(id => id).ToList()))
            .ToList());

    var bytes = SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot)));
    return Convert.ToHexString(bytes);
  }

  private sealed record TrainingPlanTarget(Guid? GroupId);
  private sealed record MemberRow(
      int Id,
      int SeasonCategoryOrder,
      string SeasonCategoryName,
      string TrainingTypeName,
      DateOnly From,
      DateOnly To,
      string DayName,
      TimeOnly TimeFrom,
      TimeOnly TimeTo,
      int LocationId);
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
      int LocationId,
      IReadOnlyList<int> CoachIds);
}
