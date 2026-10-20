using SportSys.Database.Models.sport;

namespace SportSys.Contract.Services;

internal static class CoachAssignmentQueryExtensions
{
    public static IQueryable<Training> FilterByCoachIds(
        this IQueryable<Training> query,
        IReadOnlyCollection<int>? coachIds)
        => coachIds is { Count: > 0 }
            ? query.Where(training =>
                training.CoachTrainings.Any(assignment => coachIds.Contains(assignment.CoachId)))
            : query;

    public static IQueryable<TrainingPlan> FilterByCoachIds(
        this IQueryable<TrainingPlan> query,
        IReadOnlyCollection<int>? coachIds)
        => coachIds is { Count: > 0 }
            ? query.Where(plan => plan.CoachTrainingPlans.Any(assignment =>
                coachIds.Contains(assignment.CoachId)
                && assignment.ValidFrom <= plan.To
                && assignment.ValidTo >= plan.From))
            : query;

    public static IQueryable<Match> FilterByCoachIds(
        this IQueryable<Match> query,
        IReadOnlyCollection<int>? coachIds)
        => coachIds is { Count: > 0 }
            ? query.Where(match =>
                match.CoachMatches.Any(assignment => coachIds.Contains(assignment.CoachId)))
            : query;

    public static IQueryable<TrainingRequirement> FilterByCoachIds(
        this IQueryable<TrainingRequirement> query,
        IReadOnlyCollection<int>? coachIds)
        => coachIds is { Count: > 0 }
            ? query.Where(requirement => requirement.CoachTrainingRequirements
                .Any(assignment => coachIds.Contains(assignment.CoachId)))
            : query;

    public static IQueryable<MatchRequirement> FilterByCoachIds(
        this IQueryable<MatchRequirement> query,
        IReadOnlyCollection<int>? coachIds)
        => coachIds is { Count: > 0 }
            ? query.Where(requirement => requirement.CoachMatchRequirements
                .Any(assignment => coachIds.Contains(assignment.CoachId)))
            : query;
}
