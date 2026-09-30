using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using DbTeam = SportSys.Database.Models.sport.Team;

namespace SportSys.Contract.Services;

public sealed class HomeLocationUnavailableException : InvalidOperationException
{
    public HomeLocationUnavailableException(string message)
        : base(message)
    {
    }
}

public class TeamService
{
    private readonly SportSysDbContext _db;

    public TeamService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<TeamListItem>> GetAllAsync(
        string? search = null,
        bool? isActive = true,
        CancellationToken ct = default)
    {
        var query = _db.Teams.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t =>
                t.Code.Contains(search) ||
                t.Name.Contains(search) ||
                t.City.Contains(search));

        if (isActive.HasValue)
            query = query.Where(t => t.IsActive == isActive.Value);

        return await query
            .OrderBy(t => t.Name)
            .Select(t => new TeamListItem
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                City = t.City,
                HomeLocationName = t.HomeLocation != null ? t.HomeLocation.Name : null,
                IsActive = t.IsActive,
            })
            .ToListAsync(ct);
    }

    public async Task<TeamDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Teams
            .Where(t => t.Id == id)
            .Select(t => new TeamDto
            {
                Id = t.Id,
                Code = t.Code,
                Name = t.Name,
                Address = t.Address,
                City = t.City,
                HomeLocationId = t.HomeLocationId,
                IsActive = t.IsActive,
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<TeamDto> CreateAsync(TeamDto dto, CancellationToken ct = default)
    {
        await EnsureHomeLocationCanBeAssignedAsync(dto.HomeLocationId, null, ct);

        var entity = new DbTeam
        {
            Code = dto.Code!,
            Name = dto.Name!,
            Address = dto.Address!,
            City = dto.City!,
            HomeLocationId = dto.HomeLocationId,
            IsActive = dto.IsActive,
        };

        _db.Teams.Add(entity);
        await _db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task UpdateAsync(TeamDto dto, CancellationToken ct = default)
    {
        var entity = await _db.Teams.FindAsync([dto.Id], ct)
            ?? throw new InvalidOperationException($"Tým s ID {dto.Id} nebyl nalezen.");

        await EnsureHomeLocationCanBeAssignedAsync(
            dto.HomeLocationId,
            entity.HomeLocationId,
            ct);

        entity.Code = dto.Code!;
        entity.Name = dto.Name!;
        entity.Address = dto.Address!;
        entity.City = dto.City!;
        entity.HomeLocationId = dto.HomeLocationId;
        entity.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var entity = await _db.Teams.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Tým s ID {id} nebyl nalezen.");

        entity.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureHomeLocationCanBeAssignedAsync(
        int? locationId,
        int? currentLocationId,
        CancellationToken ct)
    {
        if (!locationId.HasValue)
            return;

        var location = await _db.SportLocations
            .Where(location => location.Id == locationId.Value)
            .Select(location => new { location.IsActive })
            .SingleOrDefaultAsync(ct);
        if (location is null)
            throw new HomeLocationUnavailableException(
                $"Lokalita s ID {locationId.Value} nebyla nalezena.");

        if (!location.IsActive && locationId != currentLocationId)
            throw new HomeLocationUnavailableException(
                "Jako domácí lokalitu lze vybrat pouze aktivní lokalitu.");
    }
}
