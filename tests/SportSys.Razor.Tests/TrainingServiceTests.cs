using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Database.Enums;

namespace SportSys.Razor.Tests;

public class TrainingServiceTests
{
    [Fact]
    public void ValidateCoachAssignments_AcceptsExistingAndNewAssignments()
    {
        var existingKey = new CoachTrainingAssignmentKey(
            CoachId: 1,
            TrainingId: 10,
            ParticipationTypeId: (int)EParticipationType.NoResponse);

        var result = TrainingService.ValidateCoachAssignments(
            [
                new TrainingCoachAssignmentInputDto
                {
                    ExistingCoachId = existingKey.CoachId,
                    ExistingTrainingId = existingKey.TrainingId,
                    ExistingParticipationTypeId = existingKey.ParticipationTypeId,
                    ParticipationTypeId = (int)EParticipationType.Attended,
                    Note = "Dorazí",
                },
                new TrainingCoachAssignmentInputDto
                {
                    CoachId = 2,
                    TrainingId = 11,
                    ParticipationTypeId = (int)EParticipationType.Absent,
                },
            ],
            [existingKey],
            new HashSet<int> { 10, 11 },
            new HashSet<int> { 1, 2 });

        Assert.Equal(TrainingUpdateResult.Success, result.Result);
        Assert.Collection(
            result.Assignments,
            existing =>
            {
                Assert.Equal(1, existing.CoachId);
                Assert.Equal(10, existing.TrainingId);
                Assert.Equal((int)EParticipationType.Attended, existing.ParticipationTypeId);
                Assert.Equal((int)EParticipationType.NoResponse, existing.ExistingParticipationTypeId);
                Assert.Equal("Dorazí", existing.Note);
            },
            added =>
            {
                Assert.Equal(2, added.CoachId);
                Assert.Equal(11, added.TrainingId);
                Assert.Equal((int)EParticipationType.Absent, added.ParticipationTypeId);
                Assert.Null(added.ExistingParticipationTypeId);
            });
    }

    [Fact]
    public void ValidateCoachAssignments_RejectsOmittedExistingAssignment()
    {
        var existingKey = new CoachTrainingAssignmentKey(1, 10, (int)EParticipationType.Absent);

        var result = TrainingService.ValidateCoachAssignments(
            [CreateNewAssignment(coachId: 2, trainingId: 10)],
            [existingKey],
            new HashSet<int> { 10 },
            new HashSet<int> { 1, 2 });

        Assert.Equal(TrainingUpdateResult.CoachAssignmentRemovalNotAllowed, result.Result);
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public void ValidateCoachAssignments_RejectsDuplicateCoachAcrossGroupCategories()
    {
        var result = TrainingService.ValidateCoachAssignments(
            [
                CreateNewAssignment(coachId: 1, trainingId: 10),
                CreateNewAssignment(coachId: 1, trainingId: 11),
            ],
            [],
            new HashSet<int> { 10, 11 },
            new HashSet<int> { 1 });

        Assert.Equal(TrainingUpdateResult.DuplicateCoachAssignment, result.Result);
        Assert.Empty(result.Assignments);
    }

    [Fact]
    public void ValidateCoachAssignments_RejectsForeignTrainingAndUnavailableCoach()
    {
        var foreignTraining = TrainingService.ValidateCoachAssignments(
            [CreateNewAssignment(coachId: 1, trainingId: 99)],
            [],
            new HashSet<int> { 10 },
            new HashSet<int> { 1 });
        var unavailableCoach = TrainingService.ValidateCoachAssignments(
            [CreateNewAssignment(coachId: 99, trainingId: 10)],
            [],
            new HashSet<int> { 10 },
            new HashSet<int> { 1 });

        Assert.Equal(TrainingUpdateResult.InvalidInput, foreignTraining.Result);
        Assert.Equal(TrainingUpdateResult.InvalidInput, unavailableCoach.Result);
    }

    [Fact]
    public void ValidateCoachAssignments_RejectsModifiedExistingAssignmentKey()
    {
        var existingKey = new CoachTrainingAssignmentKey(1, 10, (int)EParticipationType.Absent);
        var forgedAssignment = new TrainingCoachAssignmentInputDto
        {
            ExistingCoachId = 1,
            ExistingTrainingId = 11,
            ExistingParticipationTypeId = (int)EParticipationType.Absent,
            ParticipationTypeId = (int)EParticipationType.Attended,
        };

        var result = TrainingService.ValidateCoachAssignments(
            [forgedAssignment],
            [existingKey],
            new HashSet<int> { 10, 11 },
            new HashSet<int> { 1 });

        Assert.Equal(TrainingUpdateResult.InvalidInput, result.Result);
    }

    [Fact]
    public void CreateVersion_IncludesCoachAttendanceAndIgnoresAssignmentOrder()
    {
        var groupId = Guid.NewGuid();
        var member = CreateMember(10, "U12");
        var first = CreateAssignment(1, 10, (int)EParticipationType.Attended, "Přijde");
        var second = CreateAssignment(2, 10, (int)EParticipationType.Absent, string.Empty);
        var initial = TrainingService.CreateVersion(groupId, [member], [first, second]);
        var reversed = TrainingService.CreateVersion(groupId, [member], [second, first]);
        var changed = TrainingService.CreateVersion(
            groupId,
            [member],
            [first, CreateAssignment(2, 10, (int)EParticipationType.Attended, string.Empty)]);
        var changedCategory = TrainingService.CreateVersion(
            groupId,
            [CreateMember(10, "U14")],
            [first, second]);

        Assert.Equal(initial, reversed);
        Assert.NotEqual(initial, changed);
        Assert.NotEqual(initial, changedCategory);
    }

    private static TrainingCoachAssignmentInputDto CreateNewAssignment(int coachId, int trainingId)
        => new()
        {
            CoachId = coachId,
            TrainingId = trainingId,
            ParticipationTypeId = (int)EParticipationType.Absent,
        };

    private static TrainingEditCoachAssignmentDto CreateAssignment(
        int coachId,
        int trainingId,
        int participationTypeId,
        string note)
        => new()
        {
            CoachId = coachId,
            TrainingId = trainingId,
            ParticipationTypeId = participationTypeId,
            CoachDisplayName = $"Coach {coachId}",
            SeasonCategoryCode = "U12",
            ParticipationTypeName = "State",
            Note = note,
        };

    private static TrainingEditMemberDto CreateMember(int id, string category)
        => new()
        {
            Id = id,
            SeasonCategoryOrder = 1,
            SeasonCategoryCode = category,
            TrainingTypeName = "Ice",
            Date = new DateOnly(2026, 10, 1),
            TimeFrom = new TimeOnly(17, 0),
            TimeTo = new TimeOnly(18, 0),
            LocationId = 1,
            LocationName = "Arena",
            Note = string.Empty,
        };
}
