namespace SportSys.Contract.Models;

public abstract class SportEventDto
{
    public int Id { get; set; }
    public int SeasonId { get; set; }
    public string SeasonCategoryName { get; set; } = string.Empty;
    public int SeasonCategoryOrder { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly TimeFrom { get; set; }
    public TimeOnly TimeTo { get; set; }
    public int? DurationMinutes { get; set; }
    public string Note { get; set; } = string.Empty;
}
