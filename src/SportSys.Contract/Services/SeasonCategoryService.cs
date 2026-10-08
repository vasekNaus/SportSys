using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using DbSeasonCategory = SportSys.Database.Models.sport.SeasonCategory;

namespace SportSys.Contract.Services;

public class SeasonCategoryService
{
    private readonly SportSysDbContext _db;

    public SeasonCategoryService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<SeasonCategoryListItem>> GetAllAsync(
        string? search = null,
        bool? isActive = true,
        CancellationToken ct = default)
    {
        var query = _db.SeasonCategories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c =>
                c.Code.Contains(search) ||
                c.CompetitionCode.Contains(search) ||
                c.CompetitionTeamName.Contains(search) ||
                c.Season.Name.Contains(search));

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query
            .OrderByDescending(c => c.Season.From)
            .ThenBy(c => c.Order)
            .ThenBy(c => c.Code)
            .Select(c => new SeasonCategoryListItem
            {
                SeasonId = c.SeasonId,
                SeasonName = c.Season.Name,
                Code = c.Code,
                Order = c.Order,
                CompetitionCode = c.CompetitionCode,
                CompetitionTeamName = c.CompetitionTeamName,
                IsActive = c.IsActive,
            })
            .ToListAsync(ct);
    }

    public async Task<SeasonCategoryEditDto?> GetByIdAsync(
        int seasonId,
        string code,
        CancellationToken ct = default)
    {
        return await _db.SeasonCategories
            .Where(c => c.SeasonId == seasonId && c.Code == code)
            .Select(c => new SeasonCategoryEditDto
            {
                SeasonId = c.SeasonId,
                Code = c.Code,
                Order = c.Order,
                CompetitionCode = c.CompetitionCode,
                CompetitionTeamName = c.CompetitionTeamName,
                BirthYears = c.BirthYears,
                IsActive = c.IsActive,
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SeasonCategoryEditDto> CreateAsync(
        SeasonCategoryEditDto dto,
        CancellationToken ct = default)
    {
        var entity = new DbSeasonCategory
        {
            SeasonId = dto.SeasonId,
            Code = dto.Code!,
            Order = dto.Order,
            CompetitionCode = dto.CompetitionCode!,
            CompetitionTeamName = dto.CompetitionTeamName!,
            BirthYears = dto.BirthYears!,
            IsActive = dto.IsActive,
        };

        _db.SeasonCategories.Add(entity);
        await _db.SaveChangesAsync(ct);
        return dto;
    }

    public async Task UpdateAsync(SeasonCategoryEditDto dto, CancellationToken ct = default)
    {
        var entity = await _db.SeasonCategories.FindAsync([dto.SeasonId, dto.Code!], ct)
            ?? throw new InvalidOperationException(
                $"Kategorie {dto.Code} v sezóně {dto.SeasonId} nebyla nalezena.");

        entity.Order = dto.Order;
        entity.CompetitionCode = dto.CompetitionCode!;
        entity.CompetitionTeamName = dto.CompetitionTeamName!;
        entity.BirthYears = dto.BirthYears!;
        entity.IsActive = dto.IsActive;

        await _db.SaveChangesAsync(ct);
    }

    public async Task SetActiveAsync(
        int seasonId,
        string code,
        bool isActive,
        CancellationToken ct = default)
    {
        var entity = await _db.SeasonCategories.FindAsync([seasonId, code], ct)
            ?? throw new InvalidOperationException(
                $"Kategorie {code} v sezóně {seasonId} nebyla nalezena.");

        entity.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }
}
