using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models.hr;
using SportSys.Database.Context;
using DbCoachAttendance = SportSys.Database.Models.hr.CoachAttendance;

namespace SportSys.Contract.Services;

public class CoachAttendanceService
{
    private readonly SportSysDbContext _db;

    public CoachAttendanceService(SportSysDbContext db)
    {
        _db = db;
    }

    public async Task<List<CoachAttendanceListItem>> GetAllAsync(
        CoachAttendanceFilter filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        Validate(filter);

        var query = _db.CoachAttendances.AsNoTracking();

        if (filter.CoachId.HasValue)
            query = query.Where(e => e.CoachId == filter.CoachId.Value);

        if (filter.PeriodFromYear.HasValue && filter.PeriodFromMonth.HasValue)
        {
            var year = filter.PeriodFromYear.Value;
            var month = filter.PeriodFromMonth.Value;
            query = query.Where(e =>
                e.PeriodYear > year || (e.PeriodYear == year && e.PeriodMonth >= month));
        }

        if (filter.PeriodToYear.HasValue && filter.PeriodToMonth.HasValue)
        {
            var year = filter.PeriodToYear.Value;
            var month = filter.PeriodToMonth.Value;
            query = query.Where(e =>
                e.PeriodYear < year || (e.PeriodYear == year && e.PeriodMonth <= month));
        }

        return await query
            .OrderByDescending(e => e.PeriodYear)
            .ThenByDescending(e => e.PeriodMonth)
            .ThenByDescending(e => e.UploadedAt)
            .Select(e => new CoachAttendanceListItem
            {
                Id = e.Id,
                CoachId = e.CoachId,
                CoachDisplayName = e.Coach.DisplayName,
                CoachPersonalNumber = e.Coach.PersonalNumber,
                PeriodYear = e.PeriodYear,
                PeriodMonth = e.PeriodMonth,
                FileName = e.FileName,
                UploadedAt = e.UploadedAt,
                UploadedByDisplayName =
                    e.UserUpload.DisplayName
                    ?? e.UserUpload.UserName
                    ?? e.UserUpload.Email
                    ?? e.UserUpload.Id.ToString(),
            })
            .ToListAsync(ct);
    }

    public Task<List<CoachSelectItem>> GetCoachesAsync(CancellationToken ct = default)
    {
        return _db.Coaches
            .AsNoTracking()
            .OrderBy(e => e.DisplayName)
            .ThenBy(e => e.PersonalNumber)
            .Select(e => new CoachSelectItem
            {
                CoachId = e.Id,
                DisplayName = e.DisplayName,
                PersonalNumber = e.PersonalNumber,
            })
            .ToListAsync(ct);
    }

    public async Task<int> UploadAsync(
        CoachAttendanceUploadDto dto,
        byte[] content,
        string contentType,
        string fileName,
        int userUploadId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        Validate(dto);

        if (userUploadId <= 0)
            throw new CoachValidationException("Nahrávající uživatel není platný.");

        var validatedFile = CoachAttendanceXlsxValidator.Validate(content, contentType, fileName);

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        if (!await _db.Coaches.AnyAsync(e => e.Id == dto.CoachId, ct))
            throw new CoachValidationException("Vybraný trenér nebyl nalezen.");
        if (!await _db.Users.AnyAsync(e => e.Id == userUploadId, ct))
            throw new CoachValidationException("Nahrávající uživatel nebyl nalezen.");
        if (await _db.CoachAttendances.AnyAsync(
                e => e.CoachId == dto.CoachId
                    && e.PeriodYear == dto.PeriodYear
                    && e.PeriodMonth == dto.PeriodMonth,
                ct))
        {
            throw new CoachValidationException(
                "Pro vybraného trenéra a období již byl soubor docházky nahrán.");
        }

        var entity = new DbCoachAttendance
        {
            CoachId = dto.CoachId,
            PeriodYear = dto.PeriodYear,
            PeriodMonth = checked((byte)dto.PeriodMonth),
            FileName = validatedFile.FileName,
            ContentType = validatedFile.ContentType,
            FileContent = content.ToArray(),
            UploadedAt = DateTime.UtcNow,
            UserUploadId = userUploadId,
        };

        _db.CoachAttendances.Add(entity);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsDuplicatePeriodViolation(ex))
        {
            throw new CoachValidationException(
                "Pro vybraného trenéra a období již byl soubor docházky nahrán.",
                ex);
        }
        await transaction.CommitAsync(ct);
        return entity.Id;
    }

    public Task<CoachAttendanceFileDto?> GetFileAsync(
        int id,
        CancellationToken ct = default)
    {
        return _db.CoachAttendances
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new CoachAttendanceFileDto
            {
                Content = e.FileContent,
                ContentType = e.ContentType,
                FileName = e.FileName,
            })
            .FirstOrDefaultAsync(ct);
    }

    private static void Validate(object dto)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, true))
        {
            throw new CoachValidationException(
                results.Select(e => e.ErrorMessage ?? "Neplatná hodnota."));
        }
    }

    private static bool IsDuplicatePeriodViolation(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 2601 or 2627 } sqlException
        && sqlException.Message.Contains(
            "IX_CoachAttendance_Coach_Period",
            StringComparison.Ordinal);
}
