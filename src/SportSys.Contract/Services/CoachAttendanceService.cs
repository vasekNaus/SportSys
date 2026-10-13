using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models.hr;
using SportSys.Contract.Models;
using SportSys.Database.Context;
using SportSys.Database.Models.sport;
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
                CoachDisplayName =
                    e.Coach.DisplayName
                    ?? e.Coach.UserName
                    ?? e.Coach.Email
                    ?? e.Coach.Id.ToString(),
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
            .OrderBy(e => e.DisplayName ?? e.UserName ?? e.Email)
            .ThenBy(e => e.PersonalNumber)
            .Select(e => new CoachSelectItem
            {
                Id = e.Id,
                DisplayName = e.DisplayName ?? e.UserName ?? e.Email ?? e.Id.ToString(),
                Email = e.Email,
                PersonalNumber = e.PersonalNumber,
            })
            .ToListAsync(ct);
    }

    public Task<List<LookupSelectItem>> GetSeasonsAsync(CancellationToken ct = default)
    {
        return _db.Seasons
            .AsNoTracking()
            .OrderByDescending(e => e.From)
            .Select(e => new LookupSelectItem
            {
                Id = e.Id,
                Name = e.Name,
            })
            .ToListAsync(ct);
    }

    public async Task<CoachAttendanceImportResult> UploadAsync(
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

        var parsedFile = CoachAttendanceXlsxParser.Parse(
            content,
            contentType,
            fileName,
            dto.PeriodYear,
            dto.PeriodMonth);

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            ct);

        try
        {
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

            var season = await _db.Seasons
                .AsNoTracking()
                .Where(e => e.Id == dto.SeasonId)
                .Select(e => new { e.Id, e.From, e.To })
                .SingleOrDefaultAsync(ct)
                ?? throw new CoachValidationException("Vybraná sezona nebyla nalezena.");

            var categories = await _db.SeasonCategories
                .AsNoTracking()
                .Where(e => e.SeasonId == dto.SeasonId)
                .Select(e => new CoachAttendanceCategorySnapshot(e.Code, e.Name))
                .ToListAsync(ct);
            var trainings = await _db.Training
                .AsNoTracking()
                .Where(e => e.SeasonId == dto.SeasonId)
                .Select(e => new CoachAttendanceTrainingSnapshot(
                    e.Id,
                    e.Date,
                    e.TimeFrom,
                    e.TimeTo,
                    e.SeasonCategoryCode,
                    e.TrainingStateId))
                .ToListAsync(ct);
            var matches = await _db.Matches
                .AsNoTracking()
                .Where(e => e.SeasonId == dto.SeasonId)
                .Select(e => new CoachAttendanceMatchSnapshot(
                    e.Id,
                    e.Date,
                    e.TimeFrom,
                    e.SeasonCategoryCode,
                    e.HomeTeam.Name,
                    e.AwayTeam.Name,
                    e.MatchStateId))
                .ToListAsync(ct);
            var participationTypes = await _db.ParticipationTypes
                .AsNoTracking()
                .Select(e => new CoachAttendanceParticipationSnapshot(e.Id, e.Name))
                .ToListAsync(ct);

            var resolvedRows = CoachAttendanceImportPlanner.Resolve(
                parsedFile.Rows,
                season.From,
                season.To,
                categories,
                trainings,
                matches,
                participationTypes);

            var trainingIds = resolvedRows
                .Where(e => e.EventKind == CoachAttendanceEventKind.Training)
                .Select(e => e.EventId)
                .ToHashSet();
            var matchIds = resolvedRows
                .Where(e => e.EventKind == CoachAttendanceEventKind.Match)
                .Select(e => e.EventId)
                .ToHashSet();
            var existingTrainings = await _db.CoachTrainings
                .Where(e => e.CoachId == dto.CoachId && trainingIds.Contains(e.TrainingId))
                .ToListAsync(ct);
            var existingMatches = await _db.CoachMatches
                .Where(e => e.CoachId == dto.CoachId && matchIds.Contains(e.MatchId))
                .ToListAsync(ct);

            var consistencyErrors = FindConsistencyErrors(
                resolvedRows,
                existingTrainings,
                existingMatches);
            if (consistencyErrors.Count > 0)
                throw new CoachValidationException(consistencyErrors);

            var result = new CoachAttendanceImportResult
            {
                SeasonId = dto.SeasonId,
                TrainingCount = trainingIds.Count,
                MatchCount = matchIds.Count,
            };
            ApplyParticipationChanges(
                dto.CoachId,
                resolvedRows,
                existingTrainings,
                existingMatches,
                result);

            var entity = new DbCoachAttendance
            {
                CoachId = dto.CoachId,
                PeriodYear = dto.PeriodYear,
                PeriodMonth = checked((byte)dto.PeriodMonth),
                FileName = parsedFile.FileName,
                ContentType = parsedFile.ContentType,
                FileContent = content.ToArray(),
                UploadedAt = DateTime.UtcNow,
                UserUploadId = userUploadId,
            };
            _db.CoachAttendances.Add(entity);
            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            result.AttendanceId = entity.Id;
            return result;
        }
        catch (DbUpdateException ex) when (IsDuplicatePeriodViolation(ex))
        {
            _db.ChangeTracker.Clear();
            throw new CoachValidationException(
                "Pro vybraného trenéra a období již byl soubor docházky nahrán.",
                ex);
        }
        catch (Exception ex) when (IsRetryableConcurrencyFailure(ex))
        {
            _db.ChangeTracker.Clear();
            throw new CoachValidationException(
                "Import se nepodařilo dokončit kvůli souběžné změně. Opakujte jej prosím.",
                ex);
        }
        catch
        {
            _db.ChangeTracker.Clear();
            throw;
        }
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

    private static bool IsRetryableConcurrencyFailure(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            if (current is SqlException { Number: 1205 or 3960 })
                return true;
            if (current.InnerException is null)
                break;
        }

        return false;
    }

    private static IReadOnlyList<string> FindConsistencyErrors(
        IReadOnlyList<CoachAttendanceResolvedRow> rows,
        IReadOnlyList<CoachTraining> existingTrainings,
        IReadOnlyList<CoachMatch> existingMatches)
    {
        var errors = new List<string>();
        foreach (var row in rows)
        {
            var count = row.EventKind == CoachAttendanceEventKind.Training
                ? existingTrainings.Count(e => e.TrainingId == row.EventId)
                : existingMatches.Count(e => e.MatchId == row.EventId);
            if (count > 1)
            {
                errors.Add(
                    $"Řádek {row.RowNumber}: trenér má u události evidováno více stavů účasti.");
            }
        }

        return errors.Take(50).ToList();
    }

    private void ApplyParticipationChanges(
        int coachId,
        IReadOnlyList<CoachAttendanceResolvedRow> rows,
        List<CoachTraining> existingTrainings,
        List<CoachMatch> existingMatches,
        CoachAttendanceImportResult result)
    {
        foreach (var row in rows)
        {
            if (row.EventKind == CoachAttendanceEventKind.Training)
            {
                var existing = existingTrainings.SingleOrDefault(e => e.TrainingId == row.EventId);
                if (existing is null)
                {
                    _db.CoachTrainings.Add(new CoachTraining
                    {
                        CoachId = coachId,
                        TrainingId = row.EventId,
                        ParticipationTypeId = row.ParticipationTypeId,
                        Note = row.Note,
                    });
                    result.InsertedCount++;
                }
                else if (existing.ParticipationTypeId == row.ParticipationTypeId)
                {
                    if (existing.Note == row.Note)
                        result.UnchangedCount++;
                    else
                    {
                        existing.Note = row.Note;
                        result.UpdatedCount++;
                    }
                }
                else
                {
                    _db.CoachTrainings.Remove(existing);
                    _db.CoachTrainings.Add(new CoachTraining
                    {
                        CoachId = coachId,
                        TrainingId = row.EventId,
                        ParticipationTypeId = row.ParticipationTypeId,
                        Note = row.Note,
                    });
                    result.UpdatedCount++;
                }
            }
            else
            {
                var existing = existingMatches.SingleOrDefault(e => e.MatchId == row.EventId);
                if (existing is null)
                {
                    _db.CoachMatches.Add(new CoachMatch
                    {
                        CoachId = coachId,
                        MatchId = row.EventId,
                        ParticipationTypeId = row.ParticipationTypeId,
                        Note = row.Note,
                    });
                    result.InsertedCount++;
                }
                else if (existing.ParticipationTypeId == row.ParticipationTypeId)
                {
                    if (existing.Note == row.Note)
                        result.UnchangedCount++;
                    else
                    {
                        existing.Note = row.Note;
                        result.UpdatedCount++;
                    }
                }
                else
                {
                    _db.CoachMatches.Remove(existing);
                    _db.CoachMatches.Add(new CoachMatch
                    {
                        CoachId = coachId,
                        MatchId = row.EventId,
                        ParticipationTypeId = row.ParticipationTypeId,
                        Note = row.Note,
                    });
                    result.UpdatedCount++;
                }
            }
        }
    }
}
