namespace SportSys.Razor.Models.TrainingSchedule;

// Id odpovídá stabilním hodnotám číselníku MatchState
// (SportSys.Database.Enums.EMatchState: Plan=1, ConfirmedKis=2, Cancelled=3).
// Razor nesmí odkazovat na SportSys.Database, mapování je proto vedeno přes
// číselné Id.
public static class MatchStateVisual
{
    private static readonly IReadOnlyDictionary<int, TrainingStateVisualInfo> ById =
        new Dictionary<int, TrainingStateVisualInfo>
        {
            [1] = new("match-state-plan", "📅"),
            [2] = new("match-state-confirmed", "✅"),
            [3] = new("match-state-cancelled", "❌"),
        };

    public static TrainingStateVisualInfo? Get(int? matchStateId)
        => matchStateId.HasValue && ById.TryGetValue(matchStateId.Value, out var info)
            ? info
            : null;
}
