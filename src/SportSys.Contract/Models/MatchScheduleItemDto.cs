namespace SportSys.Contract.Models;

public sealed class MatchScheduleItemDto : SportEventDto
{
    public int LocationId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string MatchTypeName { get; set; } = string.Empty;
    public string HomeTeamName { get; set; } = string.Empty;
    public string AwayTeamName { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public bool IsHome { get; set; }
    public int? HomeGoals { get; set; }
    public int? AwayGoals { get; set; }
}
