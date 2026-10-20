namespace SportSys.Razor.Models.TrainingSchedule;

public static class CoachParticipationVisual
{
    private static readonly IReadOnlyDictionary<int, string> Icons = new Dictionary<int, string>
    {
        [1] = "✅",
        [2] = "❓",
        [3] = "❌",
        [4] = "🔗",
        [5] = "↔",
    };

    public static string GetIcon(int participationTypeId)
        => Icons.TryGetValue(participationTypeId, out var icon)
            ? icon
            : throw new InvalidOperationException(
                $"Neznámý typ účasti trenéra s ID {participationTypeId}.");
}

public sealed class CoachParticipationEventModel
{
    public required string CategoryCode { get; init; }
    public required string FullName { get; init; }
    public required string LastName { get; init; }
    public required string Icon { get; init; }
    public required string ParticipationTypeName { get; init; }
}
