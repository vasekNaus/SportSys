using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using DbLocation = SportSys.Database.Models.sport.Location;

namespace SportSys.Contract.Services;

public class SportLocationService
{
    private readonly SportSysDbContext _db;

    public SportLocationService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<LocationDto>> GetAllAsync(
        string? search = null,
        bool? isActive = true,
        CancellationToken ct = default)
    {
        var query = _db.SportLocations.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r =>
                r.Name.Contains(search) ||
                (r.City != null && r.City.Contains(search)));

        if (isActive.HasValue)
            query = query.Where(r => r.IsActive == isActive.Value);

        return await query
            .OrderBy(r => r.City)
            .ThenBy(r => r.Name)
            .Select(r => new LocationDto
            {
                Id = r.Id,
                Name = r.Name,
                Street = r.Street,
                City = r.City,
                ZipCode = r.ZipCode,
                IsActive = r.IsActive,
            })
            .ToListAsync(ct);
    }

    public async Task<LocationDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.SportLocations
            .Where(r => r.Id == id)
            .Select(r => new LocationDto
            {
                Id = r.Id,
                Name = r.Name,
                Street = r.Street,
                City = r.City,
                ZipCode = r.ZipCode,
                IsActive = r.IsActive,
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<LookupSelectItem>> GetSelectListAsync(
        int? includeId = null,
        CancellationToken ct = default)
    {
        return await _db.SportLocations
            .Where(r => r.IsActive || r.Id == includeId)
            .OrderBy(r => r.City)
            .ThenBy(r => r.Name)
            .Select(r => new LookupSelectItem
            {
                Id = r.Id,
                Name = string.IsNullOrWhiteSpace(r.City)
                    ? r.Name
                    : r.City + " – " + r.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<LocationDto> CreateAsync(LocationDto dto, CancellationToken ct = default)
    {
        var entity = new DbLocation
        {
            Name = dto.Name!,
            Street = dto.Street,
            City = dto.City,
            ZipCode = dto.ZipCode,
            IsActive = dto.IsActive,
        };
        _db.SportLocations.Add(entity);
        await _db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task UpdateAsync(LocationDto dto, CancellationToken ct = default)
    {
        var entity = await _db.SportLocations.FindAsync([dto.Id], ct)
            ?? throw new InvalidOperationException($"Lokalita s ID {dto.Id} nebyla nalezena.");

        entity.Name = dto.Name!;
        entity.Street = dto.Street;
        entity.City = dto.City;
        entity.ZipCode = dto.ZipCode;
        entity.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(int id, bool isActive, CancellationToken ct = default)
    {
        var entity = await _db.SportLocations.FindAsync([id], ct)
            ?? throw new InvalidOperationException($"Lokalita s ID {id} nebyla nalezena.");

        entity.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }
}
