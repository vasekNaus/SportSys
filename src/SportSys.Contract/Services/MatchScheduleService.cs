using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;

namespace SportSys.Contract.Services;

public sealed class MatchScheduleService
{
    private readonly SportSysDbContext _db;

    public MatchScheduleService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<MatchScheduleItemDto>> GetMatchesAsync(
        int seasonId,
        IReadOnlyCollection<string> categoryCodes,
        DateOnly dateFrom,
        DateOnly dateTo,
        IReadOnlyCollection<int>? matchStateIds = null,
        IReadOnlyCollection<int>? locationIds = null,
        IReadOnlyCollection<int>? matchTypeIds = null,
        IReadOnlyCollection<int>? coachIds = null,
        CancellationToken ct = default)
    {
        var query = _db.Matches
            .Where(match => match.SeasonId == seasonId
                && categoryCodes.Contains(match.SeasonCategoryCode)
                && match.Date >= dateFrom
                && match.Date <= dateTo);

        query = query.FilterByCoachIds(coachIds);

        if (matchStateIds is { Count: > 0 })
            query = query.Where(match => match.MatchStateId.HasValue
                && matchStateIds.Contains(match.MatchStateId.Value));

        if (locationIds is { Count: > 0 })
            query = query.Where(match => locationIds.Contains(match.LocationId));

        if (matchTypeIds is { Count: > 0 })
            query = query.Where(match => matchTypeIds.Contains(match.MatchTypeId));

        var matches = await query
            .OrderBy(match => match.Date)
            .ThenBy(match => match.TimeFrom)
            .Select(match => new MatchProjection
            {
                Id = match.Id,
                SeasonId = match.SeasonId,
                SeasonCategoryCode = match.SeasonCategoryCode,
                SeasonCategoryOrder = match.SeasonCategory.Order,
                CompetitionTeamName = match.SeasonCategory.CompetitionTeamName,
                Date = match.Date,
                TimeFrom = match.TimeFrom,
                TimeTo = match.TimeTo,
                DurationMinutes = match.DurationMinutes,
                Note = match.Note,
                LocationId = match.LocationId,
                LocationName = match.Location.Name,
                MatchTypeName = match.MatchType.Name,
                HomeTeamName = match.HomeTeam.Name,
                AwayTeamName = match.AwayTeam.Name,
                Result = match.Result,
                MatchStateId = match.MatchStateId,
                MatchStateName = match.MatchState == null ? null : match.MatchState.Name,
            })
            .ToListAsync(ct);

        var matchDtos = matches.Select(CreateDto).ToList();
        await AssignCoachParticipationsAsync(matchDtos, ct);
        return matchDtos;
    }

    private async Task AssignCoachParticipationsAsync(
        IReadOnlyList<MatchScheduleItemDto> matches,
        CancellationToken ct)
    {
        if (matches.Count == 0)
            return;

        var matchIds = matches.Select(match => match.Id).ToList();
        var participations = await _db.CoachMatches
            .Where(assignment => matchIds.Contains(assignment.MatchId))
            .Select(assignment => new
            {
                assignment.MatchId,
                assignment.CoachId,
                CoachFullName = assignment.Coach.DisplayName ?? string.Empty,
                assignment.ParticipationTypeId,
                ParticipationTypeName = assignment.ParticipationType.Name,
            })
            .ToListAsync(ct);

        var participationsByMatchId = participations.ToLookup(item => item.MatchId);
        foreach (var match in matches)
        {
            match.CoachParticipations = participationsByMatchId[match.Id]
                .OrderBy(item => item.CoachFullName)
                .ThenBy(item => item.ParticipationTypeId)
                .Select(item => new CoachParticipationDto
                {
                    CoachId = item.CoachId,
                    FullName = item.CoachFullName,
                    LastName = ExtractLastName(item.CoachFullName),
                    ParticipationTypeId = item.ParticipationTypeId,
                    ParticipationTypeName = item.ParticipationTypeName,
                })
                .ToList();
        }
    }

    public async Task<List<LookupSelectItem>> GetMatchStatesAsync(CancellationToken ct = default)
    {
        return await _db.MatchStates
            .OrderBy(state => state.Id)
            .Select(state => new LookupSelectItem
            {
                Id = state.Id,
                Name = state.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<List<LookupSelectItem>> GetMatchTypesAsync(CancellationToken ct = default)
    {
        return await _db.MatchTypes
            .OrderBy(type => type.Id)
            .Select(type => new LookupSelectItem
            {
                Id = type.Id,
                Name = type.Name,
            })
            .ToListAsync(ct);
    }

    internal static MatchScheduleItemDto CreateDto(MatchProjection match)
    {
        var homeIsOwnTeam = TeamNamesMatch(
            match.HomeTeamName,
            match.CompetitionTeamName);
        var awayIsOwnTeam = TeamNamesMatch(
            match.AwayTeamName,
            match.CompetitionTeamName);

        if (homeIsOwnTeam == awayIsOwnTeam)
        {
            throw new InvalidOperationException(
                $"Zápas {match.Id} nemá jednoznačně určený vlastní tým " +
                $"'{match.CompetitionTeamName}' mezi '{match.HomeTeamName}' a " +
                $"'{match.AwayTeamName}'.");
        }

        return new MatchScheduleItemDto
        {
            Id = match.Id,
            SeasonId = match.SeasonId,
            SeasonCategoryCode = match.SeasonCategoryCode,
            SeasonCategoryOrder = match.SeasonCategoryOrder,
            Date = match.Date,
            TimeFrom = match.TimeFrom,
            TimeTo = match.TimeTo,
            DurationMinutes = match.DurationMinutes,
            Note = match.Note,
            LocationId = match.LocationId,
            LocationName = match.LocationName,
            MatchTypeName = match.MatchTypeName,
            HomeTeamName = match.HomeTeamName,
            AwayTeamName = match.AwayTeamName,
            OpponentName = homeIsOwnTeam
                ? match.AwayTeamName
                : match.HomeTeamName,
            IsHome = homeIsOwnTeam,
            HomeGoals = match.Result?.HomeGoals,
            AwayGoals = match.Result?.AwayGoals,
            MatchStateId = match.MatchStateId,
            MatchStateName = match.MatchStateName,
        };
    }

    internal static bool TeamNamesMatch(string teamName, string competitionTeamName)
    {
        var normalizedTeamName = teamName.Trim();
        var normalizedCompetitionTeamName = competitionTeamName.Trim();

        if (normalizedTeamName.Equals(
                normalizedCompetitionTeamName,
                StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return normalizedCompetitionTeamName.Length > 2
            && normalizedCompetitionTeamName[^2] == ' '
            && "BCD".Contains(
                normalizedCompetitionTeamName[^1],
                StringComparison.OrdinalIgnoreCase)
            && normalizedTeamName.Equals(
                normalizedCompetitionTeamName[..^2].TrimEnd(),
                StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractLastName(string fullName)
    {
        var trimmed = fullName.Trim();
        if (trimmed.Length == 0)
            return string.Empty;

        var lastSpaceIndex = trimmed.LastIndexOf(' ');
        return lastSpaceIndex < 0
            ? trimmed
            : trimmed[(lastSpaceIndex + 1)..];
    }

    internal sealed class MatchProjection
    {
        public int Id { get; init; }
        public int SeasonId { get; init; }
        public required string SeasonCategoryCode { get; init; }
        public int SeasonCategoryOrder { get; init; }
        public required string CompetitionTeamName { get; init; }
        public DateOnly Date { get; init; }
        public TimeOnly TimeFrom { get; init; }
        public TimeOnly TimeTo { get; init; }
        public int? DurationMinutes { get; init; }
        public required string Note { get; init; }
        public int LocationId { get; init; }
        public required string LocationName { get; init; }
        public required string MatchTypeName { get; init; }
        public required string HomeTeamName { get; init; }
        public required string AwayTeamName { get; init; }
        public MatchResult? Result { get; init; }
        public int? MatchStateId { get; init; }
        public string? MatchStateName { get; init; }
    }
}
