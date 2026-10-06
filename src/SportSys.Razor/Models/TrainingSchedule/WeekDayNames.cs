namespace SportSys.Razor.Models.TrainingSchedule;

/// <summary>
/// Sdílené pořadí a české názvy dnů v týdnu pro filtr „Den“ na stránkách
/// Schedule a Plan, včetně pomocné metody pro filtrování kolekcí podle dne.
/// </summary>
public static class WeekDayNames
{
    public static readonly IReadOnlyList<DayOfWeek> OrderedDays =
    [
        DayOfWeek.Monday,
        DayOfWeek.Tuesday,
        DayOfWeek.Wednesday,
        DayOfWeek.Thursday,
        DayOfWeek.Friday,
        DayOfWeek.Saturday,
        DayOfWeek.Sunday,
    ];

    public static string GetFullName(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => "Pondělí",
        DayOfWeek.Tuesday => "Úterý",
        DayOfWeek.Wednesday => "Středa",
        DayOfWeek.Thursday => "Čtvrtek",
        DayOfWeek.Friday => "Pátek",
        DayOfWeek.Saturday => "Sobota",
        DayOfWeek.Sunday => "Neděle",
        _ => throw new ArgumentOutOfRangeException(nameof(day)),
    };

    /// <summary>
    /// Vrátí jen položky, jejichž den (dle <paramref name="selector"/>) je
    /// obsažen v <paramref name="selectedDays"/>. Prázdný výběr znamená
    /// „všechny dny“ a vstup se vrátí beze změny pořadí.
    /// </summary>
    public static List<T> FilterByDay<T>(
        IReadOnlyList<T> items,
        Func<T, DayOfWeek> selector,
        IReadOnlyCollection<DayOfWeek> selectedDays)
        => selectedDays.Count == 0
            ? items.ToList()
            : items.Where(item => selectedDays.Contains(selector(item))).ToList();
}
