using SportSys.Contract.Models;
using SportSys.Database.Enums;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Razor.Tests;

public class TrainingEditDtoTests
{
    [Fact]
    public void Validate_RejectsNonIncreasingTimeRange()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(17, 0));

        var results = Validate(dto);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(TrainingEditDto.TimeFrom)) &&
            result.MemberNames.Contains(nameof(TrainingEditDto.TimeTo)));
    }

    [Fact]
    public void Validate_AcceptsValidTraining()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));

        var results = Validate(dto);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_AcceptsEmptyNote()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));
        dto.Note = null;

        var results = Validate(dto);

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_RejectsMissingLocation()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));
        dto.LocationId = 0;

        var results = Validate(dto);

        Assert.Contains(results, result =>
            result.MemberNames.Contains(nameof(TrainingEditDto.LocationId)));
    }

    [Fact]
    public void Validate_RejectsCoachAssignmentWithoutCoachOrCategory()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));
        dto.CoachAssignments.Add(new TrainingCoachAssignmentInputDto
        {
            ParticipationTypeId = (int)EParticipationType.Absent,
        });

        var results = Validate(dto);

        Assert.Contains(results, result => result.ErrorMessage == "Vyberte trenéra a kategorii.");
    }

    [Fact]
    public void Validate_AcceptsExistingCoachAssignmentWithChangedAttendance()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));
        dto.CoachAssignments.Add(new TrainingCoachAssignmentInputDto
        {
            ExistingCoachId = 1,
            ExistingTrainingId = 2,
            ExistingParticipationTypeId = (int)EParticipationType.NoResponse,
            ParticipationTypeId = (int)EParticipationType.Attended,
            Note = "Dorazí",
        });

        var results = Validate(dto);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public void Validate_RejectsUnknownParticipationType(int participationTypeId)
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));
        dto.CoachAssignments.Add(new TrainingCoachAssignmentInputDto
        {
            CoachId = 1,
            TrainingId = 2,
            ParticipationTypeId = participationTypeId,
        });

        var results = Validate(dto);

        Assert.Contains(results, result => result.ErrorMessage == "Vyberte platný stav docházky.");
    }

    [Fact]
    public void Validate_RejectsCoachNoteLongerThanFiftyCharacters()
    {
        var dto = CreateDto(
            new TimeOnly(17, 0),
            new TimeOnly(18, 0));
        dto.CoachAssignments.Add(new TrainingCoachAssignmentInputDto
        {
            CoachId = 1,
            TrainingId = 2,
            ParticipationTypeId = (int)EParticipationType.Absent,
            Note = new string('x', 51),
        });

        var results = Validate(dto);

        Assert.Contains(
            results,
            result => result.ErrorMessage == "Poznámka trenéra nesmí přesáhnout 50 znaků.");
    }

    private static TrainingEditDto CreateDto(TimeOnly timeFrom, TimeOnly timeTo)
        => new()
        {
            Id = 1,
            Date = new DateOnly(2026, 9, 8),
            TimeFrom = timeFrom,
            TimeTo = timeTo,
            LocationId = 1,
            Note = string.Empty,
        };

    private static List<ValidationResult> Validate(TrainingEditDto dto)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(dto, new ValidationContext(dto), results, true);
        for (var index = 0; index < dto.CoachAssignments.Count; index++)
        {
            var assignment = dto.CoachAssignments[index];
            var assignmentResults = new List<ValidationResult>();
            Validator.TryValidateObject(
                assignment,
                new ValidationContext(assignment),
                assignmentResults,
                validateAllProperties: true);
            results.AddRange(assignmentResults.Select(result => new ValidationResult(
                result.ErrorMessage,
                result.MemberNames.Select(
                    memberName =>
                        $"{nameof(TrainingEditDto.CoachAssignments)}[{index}].{memberName}"))));
        }

        return results;
    }
}
