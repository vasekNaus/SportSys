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
        .ThenBy(plan => plan.SeasonCategoryCode)
        .ThenBy(plan => plan.Id)
        .Select(plan => new MemberRow(
            plan.Id,
            plan.SeasonCategory.Order,
            plan.SeasonCategoryCode,
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
    var coachAssignmentsByMember = await LoadCoachAssignmentsByMemberAsync(memberIds, ct);

    var members = memberRows
        .Select(row => new TrainingPlanEditMemberDto
        {
          Id = row.Id,
          SeasonCategoryOrder = row.SeasonCategoryOrder,
          SeasonCategoryCode = row.SeasonCategoryCode,
          TrainingTypeName = row.TrainingTypeName,
          From = row.From,
          To = row.To,
          DayName = row.DayName,
          TimeFrom = row.TimeFrom,
          TimeTo = row.TimeTo,
          LocationId = row.LocationId,
          CoachAssignments = coachAssignmentsByMember.TryGetValue(row.Id, out var assignments)
                ? assignments
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
        CoachAssignments = selected.CoachAssignments
            .Select(ToInput)
            .ToList(),
        MemberCoachAssignments = target.GroupId.HasValue
                ? members
                    .Select(member => new TrainingPlanMemberCoachInputDto
                    {
                      TrainingPlanId = member.Id,
                      CoachAssignments = member.CoachAssignments
                          .Select(ToInput)
                          .ToList(),
                    })
                    .ToList()
                : [],
      },
      Members = members,
      AvailableCoaches = availableCoaches,
      CoachRoles = await GetCoachRolesAsync(ct),
      IsGrouped = target.GroupId.HasValue,
      CanEdit = HaveConsistentEditableValues(members),
    };
  }

  private static TrainingPlanCoachAssignmentInputDto ToInput(
      TrainingPlanCoachAssignmentDto assignment)
      => new()
      {
        CoachId = assignment.CoachId,
        CoachRoleId = assignment.CoachRoleId,
      };

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

  private async Task<Dictionary<int, List<TrainingPlanCoachAssignmentDto>>> LoadCoachAssignmentsByMemberAsync(
      IReadOnlyCollection<int> memberIds,
      CancellationToken ct)
  {
    var assignments = await _db.CoachTrainingPlans
        .AsNoTracking()
        .Where(assignment => memberIds.Contains(assignment.TrainingPlanId))
        .OrderByDescending(assignment => assignment.ValidFrom)
        .ThenByDescending(assignment => assignment.ValidTo)
        .ThenBy(assignment => assignment.Coach.DisplayName ?? assignment.Coach.UserName ?? assignment.Coach.Email)
        .ThenBy(assignment => assignment.CoachId)
        .Select(assignment => new
        {
          assignment.TrainingPlanId,
          Assignment = new TrainingPlanCoachAssignmentDto
          {
            CoachId = assignment.CoachId,
            CoachDisplayName = assignment.Coach.DisplayName
                ?? assignment.Coach.UserName
                ?? assignment.Coach.Email
                ?? assignment.CoachId.ToString(),
            CoachRoleId = assignment.CoachRoleId,
            CoachRoleName = assignment.CoachRole.Name,
          },
        })
        .ToListAsync(ct);

    return assignments
        .GroupBy(assignment => assignment.TrainingPlanId)
        .ToDictionary(
            group => group.Key,
            group => group
                .GroupBy(assignment => assignment.Assignment.CoachId)
                .Select(coachAssignments => coachAssignments.First().Assignment)
                .ToList());
  }

  private Task<List<LookupSelectItem>> GetCoachRolesAsync(CancellationToken ct)
      => _db.CoachRoles
          .AsNoTracking()
          .OrderBy(role => role.Order)
          .ThenBy(role => role.Id)
          .Select(role => new LookupSelectItem { Id = role.Id, Name = role.Name })
          .ToListAsync(ct);

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
        .Include(assignment => assignment.Coach)
        .Include(assignment => assignment.CoachRole)
        .Where(assignment => memberIds.Contains(assignment.TrainingPlanId))
        .ToListAsync(ct);
    var assignmentsByPlan = existingAssignments.ToLookup(assignment => assignment.TrainingPlanId);

    var currentMembers = plans
        .Select(plan => new TrainingPlanEditMemberDto
        {
          Id = plan.Id,
          SeasonCategoryOrder = plan.SeasonCategory.Order,
          SeasonCategoryCode = plan.SeasonCategoryCode,
          TrainingTypeName = plan.TrainingType.Name,
          From = plan.From,
          To = plan.To,
          DayName = plan.DayName,
          TimeFrom = plan.TimeFrom,
          TimeTo = plan.TimeTo,
          LocationId = plan.LocationId,
          CoachAssignments = assignmentsByPlan[plan.Id]
                .GroupBy(assignment => assignment.CoachId)
                .Select(group => group
                    .OrderByDescending(assignment => assignment.ValidFrom)
                    .ThenByDescending(assignment => assignment.ValidTo)
                    .First())
                .Select(assignment => new TrainingPlanCoachAssignmentDto
                {
                  CoachId = assignment.CoachId,
                  CoachDisplayName = assignment.Coach.DisplayName
                      ?? assignment.Coach.UserName
                      ?? assignment.Coach.Email
                      ?? assignment.CoachId.ToString(),
                  CoachRoleId = assignment.CoachRoleId,
                  CoachRoleName = assignment.CoachRole.Name,
                })
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
    if (groupId.HasValue &&
        dto.MemberCoachAssignments
            .GroupBy(assignment => assignment.TrainingPlanId)
            .Any(group => group.Count() > 1))
    {
      return TrainingPlanUpdateResult.InvalidInput;
    }

    var requestedCoachAssignmentsByPlan = BuildRequestedCoachAssignmentsByPlan(
        dto,
        groupId,
        memberIds);

    var availableRoleIds = await _db.CoachRoles
        .Select(role => role.Id)
        .ToHashSetAsync(ct);
    var assignmentValidation = ValidateCoachAssignments(
        requestedCoachAssignmentsByPlan,
        availableCoachIds.ToHashSet(),
        availableRoleIds);
    if (assignmentValidation != TrainingPlanUpdateResult.Success)
      return assignmentValidation;

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
      var selectedAssignments = requestedCoachAssignmentsByPlan.TryGetValue(plan.Id, out var assignments)
          ? assignments
          : [];

      SynchronizeCoachAssignments(
          plan.Id,
          dto.From,
          dto.To,
          selectedAssignments,
          assignmentsByPlan[plan.Id].ToList());
    }

    await _db.SaveChangesAsync(ct);
    await transaction.CommitAsync(ct);
    return TrainingPlanUpdateResult.Success;
  }

  /// <summary>
  /// Sestaví seznam přiřazení trenérů a rolí pro každý `TrainingPlanId` ve
  /// skupině. U spojeného plánu se vstup čte z
  /// <see cref="TrainingPlanEditDto.MemberCoachAssignments"/>, a to jen
  /// pro ID skutečně existujících členů (<paramref name="memberIds"/>) —
  /// položky pro cizí/neexistující ID z requestu se ignorují. U
  /// nespojeného plánu se použije <see cref="TrainingPlanEditDto.CoachAssignments"/>
  /// pro jediný <c>dto.Id</c>.
  /// </summary>
  internal static Dictionary<int, List<TrainingPlanCoachAssignmentInputDto>> BuildRequestedCoachAssignmentsByPlan(
      TrainingPlanEditDto dto,
      Guid? groupId,
      IReadOnlyCollection<int> memberIds)
  {
    if (!groupId.HasValue)
    {
      return new Dictionary<int, List<TrainingPlanCoachAssignmentInputDto>>
      {
        [dto.Id] = dto.CoachAssignments ?? [],
      };
    }

    var memberIdSet = memberIds.ToHashSet();
    var result = memberIds.ToDictionary(
        id => id,
        _ => new List<TrainingPlanCoachAssignmentInputDto>());

    foreach (var assignment in dto.MemberCoachAssignments)
    {
      if (!memberIdSet.Contains(assignment.TrainingPlanId))
        continue;

      result[assignment.TrainingPlanId].AddRange(assignment.CoachAssignments);
    }

    return result;
  }

  internal static TrainingPlanUpdateResult ValidateCoachAssignments(
      IReadOnlyDictionary<int, List<TrainingPlanCoachAssignmentInputDto>> assignmentsByPlan,
      IReadOnlySet<int> availableCoachIds,
      IReadOnlySet<int> availableRoleIds)
  {
    foreach (var assignments in assignmentsByPlan.Values)
    {
      var seenCoachIds = new HashSet<int>();
      foreach (var assignment in assignments)
      {
        if (assignment is null ||
            assignment.CoachId is null or <= 0 ||
            assignment.CoachRoleId is null or <= 0 ||
            !availableCoachIds.Contains(assignment.CoachId.Value) ||
            !availableRoleIds.Contains(assignment.CoachRoleId.Value))
        {
          return TrainingPlanUpdateResult.InvalidInput;
        }

        if (!seenCoachIds.Add(assignment.CoachId.Value))
          return TrainingPlanUpdateResult.DuplicateCoachAssignment;
      }
    }

    return TrainingPlanUpdateResult.Success;
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
      IReadOnlyCollection<TrainingPlanCoachAssignmentInputDto> selectedAssignments,
      IReadOnlyCollection<CoachTrainingPlan> existingAssignments)
  {
    var roleByCoachId = selectedAssignments.ToDictionary(
        assignment => assignment.CoachId!.Value,
        assignment => assignment.CoachRoleId!.Value);
    var diff = ComputeCoachAssignmentDiff(
        roleByCoachId.Keys.ToList(),
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
            CreateCoachTrainingPlan(
                trainingPlanId,
                validFrom,
                validTo,
                coachId,
                roleByCoachId[coachId]));
      else
        matching.CoachRoleId = roleByCoachId[coachId];
    }

    _db.CoachTrainingPlans.AddRange(
        diff.ToAdd.Select(coachId => CreateCoachTrainingPlan(
            trainingPlanId,
            validFrom,
            validTo,
            coachId,
            roleByCoachId[coachId])));
  }

  private static CoachTrainingPlan CreateCoachTrainingPlan(
      int trainingPlanId,
      DateOnly validFrom,
      DateOnly validTo,
      int coachId,
      int coachRoleId)
      => new()
      {
        CoachId = coachId,
        TrainingPlanId = trainingPlanId,
        ValidFrom = validFrom,
        ValidTo = validTo,
        CoachRoleId = coachRoleId,
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
         !string.IsNullOrEmpty(dto.OriginalVersion) &&
         dto.CoachAssignments is not null &&
         dto.CoachAssignments.All(assignment => assignment is not null) &&
         dto.MemberCoachAssignments is not null &&
         dto.MemberCoachAssignments.All(assignment =>
             assignment is not null && assignment.CoachAssignments is not null);

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
                member.CoachAssignments
                    .OrderBy(assignment => assignment.CoachId)
                    .Select(assignment => new TrainingPlanAssignmentVersion(
                        assignment.CoachId,
                        assignment.CoachRoleId))
                    .ToList()))
            .ToList());

    var bytes = SHA256.HashData(
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot)));
    return Convert.ToHexString(bytes);
  }

  private sealed record TrainingPlanTarget(Guid? GroupId);
  private sealed record MemberRow(
      int Id,
      int SeasonCategoryOrder,
      string SeasonCategoryCode,
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
  private sealed record TrainingPlanAssignmentVersion(int CoachId, int CoachRoleId);
  private sealed record TrainingPlanMemberVersion(
      int Id,
      DateOnly From,
      DateOnly To,
      string DayName,
      TimeOnly TimeFrom,
      TimeOnly TimeTo,
      int LocationId,
      IReadOnlyList<TrainingPlanAssignmentVersion> CoachAssignments);
}
