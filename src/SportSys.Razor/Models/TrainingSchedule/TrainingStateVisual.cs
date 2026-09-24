namespace SportSys.Razor.Models.TrainingSchedule;

// Id odpovídá stabilním hodnotám číselníku TrainingState
// (SportSys.Database.Enums.ETrainingState: Plan=1, ConfirmedKis=2, KisOnly=3,
// TimeChanged=4, Cancelled=5, DateChanged=6, ArenaFault=7). Razor nesmí
// odkazovat na SportSys.Database, mapování je proto vedeno přes číselné Id.
public static class TrainingStateVisual
{
    private static readonly IReadOnlyDictionary<int, TrainingStateVisualInfo> ById =
        new Dictionary<int, TrainingStateVisualInfo>
        {
            [1] = new("training-state-plan", "📅"),
            [2] = new("training-state-confirmed", "✅"),
            [3] = new("training-state-kis-only", "📝"),
            [4] = new("training-state-time-change", "🕒"),
            [5] = new("training-state-cancelled", "❌"),
            [6] = new("training-state-date-change", "🔄"),
            [7] = new("training-state-zs-failure", "⚠️"),
        };

    public static TrainingStateVisualInfo? Get(int? trainingStateId)
        => trainingStateId.HasValue && ById.TryGetValue(trainingStateId.Value, out var info)
            ? info
            : null;

    // Sentinel pro blok se spojenými tréninky s rozdílnými stavy. Není
    // hodnotou číselníku TrainingState, proto stojí mimo slovník ById.
    public const string UnknownIcon = "❓";
}

public readonly record struct TrainingStateVisualInfo(string CssClass, string Icon);
