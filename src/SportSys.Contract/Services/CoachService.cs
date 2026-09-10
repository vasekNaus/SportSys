using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Contract.Models.hr;
using SportSys.Database.Context;
using DbCoach = SportSys.Database.Models.hr.Coach;
using DbCoachContract = SportSys.Database.Models.hr.CoachContract;
using DbCoachLicense = SportSys.Database.Models.hr.CoachLicense;
using DbCoachSetting = SportSys.Database.Models.hr.CoachSetting;

namespace SportSys.Contract.Services;

public class CoachService
{
    public const int MaxPhotoSizeBytes = 5 * 1024 * 1024;

    private readonly SportSysDbContext _db;
    private readonly ILookupNormalizer _normalizer;

    public CoachService(SportSysDbContext db, ILookupNormalizer normalizer)
    {
        _db = db;
        _normalizer = normalizer;
    }

    public async Task<List<CoachListItem>> GetAllAsync(
        CoachFilter filter,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var today = DateOnly.FromDateTime(DateTime.Today);
        var seasonId = filter.SeasonId ?? await _db.Seasons
            .Where(s => s.From <= today && s.To >= today)
            .OrderByDescending(s => s.From)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync(ct);

        var query = _db.Coaches.AsNoTracking();
        var search = filter.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(c =>
                (c.DisplayName != null && c.DisplayName.Contains(search)) ||
                (c.UserName != null && c.UserName.Contains(search)) ||
                (c.Email != null && c.Email.Contains(search)) ||
                c.PersonalNumber.Contains(search));
        }

        if (filter.SeasonId.HasValue)
            query = query.Where(c => c.Contracts.Any(x => x.SeasonId == filter.SeasonId.Value));

        if (filter.ActiveContractOnly)
        {
            query = seasonId.HasValue
                ? query.Where(c => c.Contracts.Any(x => x.IsActive && x.SeasonId == seasonId.Value))
                : query.Where(c => c.Contracts.Any(x => x.IsActive));
        }

        return await query
            .OrderBy(c => c.DisplayName ?? c.UserName ?? c.Email)
            .Select(c => new CoachListItem
            {
                Id = c.Id,
                DisplayName = c.DisplayName ?? c.UserName ?? c.Email ?? c.Id.ToString(),
                Email = c.Email,
                PersonalNumber = c.PersonalNumber,
                HasPhoto = c.Photo != null,
                CurrentLicenseNames = c.Licenses
                    .Where(x => x.ValidFrom <= today && (x.ValidTo == null || x.ValidTo >= today))
                    .OrderBy(x => x.CoachLicenseType.Name)
                    .Select(x => x.CoachLicenseType.Name)
                    .ToList(),
                ActiveContractNames = c.Contracts
                    .Where(x => x.IsActive && (!seasonId.HasValue || x.SeasonId == seasonId.Value))
                    .OrderByDescending(x => x.Season.From)
                    .ThenBy(x => x.ContractType)
                    .Select(x => x.Season.Name + " – " +
                        (x.ContractType == (byte)ECoachContractType.Dpp ? "DPP" : "OSVČ"))
                    .ToList(),
            })
            .ToListAsync(ct);
    }

    public async Task<CoachDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        return await _db.Coaches
            .AsNoTracking()
            .Where(c => c.Id == id)
            .Select(c => new CoachDetailDto
            {
                Id = c.Id,
                UserName = c.UserName,
                DisplayName = c.DisplayName,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                PersonalNumber = c.PersonalNumber,
                //BirthNumber = c.BirthNumber,
                HasPhoto = c.Photo != null,
                PhotoFileName = c.PhotoFileName,
                Settings = c.Settings
                    .OrderByDescending(x => x.ValidFrom)
                    .Select(x => new CoachSettingDto
                    {
                        Id = x.Id,
                        CoachId = x.CoachId,
                        ValidFrom = x.ValidFrom,
                        ValidTo = x.ValidTo,
                        BankAccountPrefix = x.BankAccountPrefix,
                        BankAccountNumber = x.BankAccountNumber,
                        BankCode = x.BankCode,
                        Street = x.Street,
                        City = x.City,
                        ZipCode = x.ZipCode,
                        HealthInsuranceCode = x.HealthInsuranceCode,
                    })
                    .ToList(),
                Licenses = c.Licenses
                    .OrderByDescending(x => x.ValidFrom)
                    .Select(x => new CoachLicenseDto
                    {
                        Id = x.Id,
                        CoachId = x.CoachId,
                        CoachLicenseTypeId = x.CoachLicenseTypeId,
                        CoachLicenseTypeName = x.CoachLicenseType.Name,
                        ValidFrom = x.ValidFrom,
                        ValidTo = x.ValidTo,
                    })
                    .ToList(),
                Contracts = c.Contracts
                    .OrderByDescending(x => x.Season.From)
                    .ThenBy(x => x.ContractType)
                    .Select(x => new CoachContractDto
                    {
                        Id = x.Id,
                        CoachId = x.CoachId,
                        SeasonId = x.SeasonId,
                        SeasonName = x.Season.Name,
                        ContractType = (ECoachContractType)x.ContractType,
                        RewardAmount = x.RewardAmount,
                        IsActive = x.IsActive,
                    })
                    .ToList(),
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<UserSelectItem>> GetAvailableUsersAsync(
        int? includeUserId = null,
        CancellationToken ct = default)
    {
        return await _db.Users
            .AsNoTracking()
            .Where(u => !_db.Coaches.Any(c => c.Id == u.Id) || u.Id == includeUserId)
            .OrderBy(u => u.DisplayName ?? u.UserName ?? u.Email)
            .Select(u => new UserSelectItem
            {
                Id = u.Id,
                DisplayName = u.DisplayName ?? u.UserName ?? u.Email ?? u.Id.ToString(),
                Email = u.Email,
            })
            .ToListAsync(ct);
    }

    public async Task<int> CreateAsync(
        int userId,
        CoachDetailDto dto,
        CancellationToken ct = default)
    {
        if (userId <= 0)
            throw new CoachValidationException("Vybraný uživatel není platný.");

        Validate(dto);
        dto.PersonalNumber = NormalizeRequired(dto.PersonalNumber, "Osobní číslo");
        dto.IdentificationNumber = NormalizeBirthNumber(dto.IdentificationNumber);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var user = await _db.Users.SingleOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new CoachValidationException("Vybraný uživatel nebyl nalezen.");

        if (await _db.Coaches.AnyAsync(c => c.Id == userId, ct))
            throw new CoachValidationException("Vybraný uživatel již je trenérem.");

        await EnsureUniqueBasicDataAsync(dto.PersonalNumber, dto.IdentificationNumber, null, ct);
        UpdateUserForPromotion(user, dto);
        await SaveChangesAsync("Údaje uživatele se nepodařilo uložit.", ct);

        try
        {
            // EF neumí přidat derived řádek k již existující instanci základního typu.
            var affectedRows = await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""
                INSERT INTO [hr].[Coach] ([Id], [PersonalNumber], [BirthNumber])
                VALUES ({user.Id}, {dto.PersonalNumber}, {dto.IdentificationNumber})
                """,
                ct);

            if (affectedRows != 1)
                throw new CoachValidationException("Trenérský profil se nepodařilo vytvořit.");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            throw new CoachValidationException(GetCoachDuplicateMessage(exception), exception);
        }

        await transaction.CommitAsync(ct);
        _db.Entry(user).State = EntityState.Detached;
        return user.Id;
    }

    public async Task UpdateBasicAsync(CoachDetailDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        dto.PersonalNumber = NormalizeRequired(dto.PersonalNumber, "Osobní číslo");
        dto.IdentificationNumber = NormalizeBirthNumber(dto.IdentificationNumber);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var coach = await _db.Coaches
            .SingleOrDefaultAsync(c => c.Id == dto.Id, ct)
            ?? throw new CoachValidationException("Trenér nebyl nalezen.");

        await EnsureUniqueBasicDataAsync(dto.PersonalNumber, dto.IdentificationNumber, coach.Id, ct);
        UpdateUser(coach, dto);
        coach.PersonalNumber = dto.PersonalNumber;
        coach.IdentificationNumber = dto.IdentificationNumber;

        await SaveChangesAsync("Základní údaje se nepodařilo uložit kvůli konfliktu uložených údajů.", ct);
        await transaction.CommitAsync(ct);
    }

    public Task<CoachPhotoDto?> GetPhotoAsync(int coachId, CancellationToken ct = default)
    {
        return _db.Coaches
            .AsNoTracking()
            .Where(c => c.Id == coachId && c.Photo != null)
            .Select(c => new CoachPhotoDto
            {
                Content = c.Photo!,
                ContentType = c.PhotoContentType!,
                FileName = c.PhotoFileName!,
            })
            .FirstOrDefaultAsync(ct);
    }

    public async Task SetPhotoAsync(
        int coachId,
        byte[] content,
        string contentType,
        string fileName,
        CancellationToken ct = default)
    {
        var normalizedContentType = ValidatePhoto(content, contentType);
        var safeFileName = Path.GetFileName(fileName?.Trim());
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Length > 255)
            throw new CoachValidationException("Název souboru fotografie je neplatný.");

        var coach = await GetCoachAsync(coachId, ct);
        coach.Photo = content.ToArray();
        coach.PhotoContentType = normalizedContentType;
        coach.PhotoFileName = safeFileName;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeletePhotoAsync(int coachId, CancellationToken ct = default)
    {
        var coach = await GetCoachAsync(coachId, ct);
        coach.Photo = null;
        coach.PhotoContentType = null;
        coach.PhotoFileName = null;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CreateContractAsync(CoachContractDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        await EnsureCoachExistsAsync(dto.CoachId, ct);
        if (!await _db.Seasons.AnyAsync(s => s.Id == dto.SeasonId, ct))
            throw new CoachValidationException("Vybraná sezóna nebyla nalezena.");

        var entity = new DbCoachContract
        {
            CoachId = dto.CoachId,
            SeasonId = dto.SeasonId,
            ContractType = (byte)dto.ContractType,
            RewardAmount = dto.RewardAmount,
            IsActive = dto.IsActive,
        };
        _db.CoachContracts.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateContractAsync(CoachContractDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        var entity = await _db.CoachContracts
            .SingleOrDefaultAsync(x => x.Id == dto.Id && x.CoachId == dto.CoachId, ct)
            ?? throw new CoachValidationException("Smlouva nebyla nalezena nebo nepatří zadanému trenérovi.");

        if (!await _db.Seasons.AnyAsync(s => s.Id == dto.SeasonId, ct))
            throw new CoachValidationException("Vybraná sezóna nebyla nalezena.");

        entity.SeasonId = dto.SeasonId;
        entity.ContractType = (byte)dto.ContractType;
        entity.RewardAmount = dto.RewardAmount;
        entity.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task SetContractActiveAsync(
        int coachId,
        int contractId,
        bool isActive,
        CancellationToken ct = default)
    {
        var entity = await _db.CoachContracts
            .SingleOrDefaultAsync(x => x.Id == contractId && x.CoachId == coachId, ct)
            ?? throw new CoachValidationException("Smlouva nebyla nalezena nebo nepatří zadanému trenérovi.");

        entity.IsActive = isActive;
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> CreateSettingAsync(CoachSettingDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        NormalizeSetting(dto);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await EnsureCoachExistsAsync(dto.CoachId, ct);

        var openSettings = await _db.CoachSettings
            .Where(x => x.CoachId == dto.CoachId && x.ValidTo == null)
            .OrderByDescending(x => x.ValidFrom)
            .ToListAsync(ct);

        if (openSettings.Count > 1)
            throw new CoachValidationException("Trenér má více otevřených intervalů nastavení.");

        var previousOpen = openSettings.SingleOrDefault();
        if (previousOpen != null)
        {
            if (previousOpen.ValidFrom >= dto.ValidFrom)
                throw new CoachValidationException("Nové nastavení musí začínat po začátku otevřeného intervalu.");

            previousOpen.ValidTo = dto.ValidFrom.AddDays(-1);
        }

        if (await HasSettingOverlapAsync(
                dto.CoachId, dto.ValidFrom, dto.ValidTo, previousOpen?.Id, ct))
            throw new CoachValidationException("Interval nastavení se překrývá s existujícím intervalem.");

        var entity = new DbCoachSetting
        {
            CoachId = dto.CoachId,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
            BankAccountPrefix = dto.BankAccountPrefix,
            BankAccountNumber = dto.BankAccountNumber,
            BankCode = dto.BankCode,
            Street = dto.Street,
            City = dto.City,
            ZipCode = dto.ZipCode,
            HealthInsuranceCode = dto.HealthInsuranceCode,
        };
        _db.CoachSettings.Add(entity);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return entity.Id;
    }

    public async Task UpdateSettingAsync(CoachSettingDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        NormalizeSetting(dto);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = await _db.CoachSettings
            .SingleOrDefaultAsync(x => x.Id == dto.Id && x.CoachId == dto.CoachId, ct)
            ?? throw new CoachValidationException("Nastavení nebylo nalezeno nebo nepatří zadanému trenérovi.");

        if (await HasSettingOverlapAsync(dto.CoachId, dto.ValidFrom, dto.ValidTo, dto.Id, ct))
            throw new CoachValidationException("Interval nastavení se překrývá s existujícím intervalem.");

        CopySetting(dto, entity);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<int> CreateLicenseAsync(CoachLicenseDto dto, CancellationToken ct = default)
    {
        Validate(dto);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        await EnsureCoachExistsAsync(dto.CoachId, ct);
        await EnsureLicenseTypeExistsAsync(dto.CoachLicenseTypeId, ct);

        if (await HasLicenseOverlapAsync(
                dto.CoachId, dto.CoachLicenseTypeId, dto.ValidFrom, dto.ValidTo, null, ct))
        {
            throw new CoachValidationException(
                "Interval licence se překrývá s existující licencí stejného typu.");
        }

        var entity = new DbCoachLicense
        {
            CoachId = dto.CoachId,
            CoachLicenseTypeId = dto.CoachLicenseTypeId,
            ValidFrom = dto.ValidFrom,
            ValidTo = dto.ValidTo,
        };
        _db.CoachLicenses.Add(entity);
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return entity.Id;
    }

    public async Task UpdateLicenseAsync(CoachLicenseDto dto, CancellationToken ct = default)
    {
        Validate(dto);

        await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var entity = await _db.CoachLicenses
            .SingleOrDefaultAsync(x => x.Id == dto.Id && x.CoachId == dto.CoachId, ct)
            ?? throw new CoachValidationException("Licence nebyla nalezena nebo nepatří zadanému trenérovi.");

        await EnsureLicenseTypeExistsAsync(dto.CoachLicenseTypeId, ct);
        if (await HasLicenseOverlapAsync(
                dto.CoachId, dto.CoachLicenseTypeId, dto.ValidFrom, dto.ValidTo, dto.Id, ct))
        {
            throw new CoachValidationException(
                "Interval licence se překrývá s existující licencí stejného typu.");
        }

        entity.CoachLicenseTypeId = dto.CoachLicenseTypeId;
        entity.ValidFrom = dto.ValidFrom;
        entity.ValidTo = dto.ValidTo;
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public Task<List<LookupSelectItem>> GetSeasonsAsync(CancellationToken ct = default)
    {
        return _db.Seasons
            .AsNoTracking()
            .OrderByDescending(s => s.From)
            .Select(s => new LookupSelectItem { Id = s.Id, Name = s.Name })
            .ToListAsync(ct);
    }

    public Task<List<CoachLicenseTypeSelectItem>> GetLicenseTypesAsync(CancellationToken ct = default)
    {
        return _db.CoachLicenseTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Id)
            .Select(x => new CoachLicenseTypeSelectItem
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
            })
            .ToListAsync(ct);
    }

    private static void Validate(object dto)
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), results, true))
        {
            throw new CoachValidationException(
                results.Select(x => x.ErrorMessage ?? "Neplatná hodnota."));
        }
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        var normalized = value?.Trim() ?? string.Empty;
        return normalized.Length > 0
            ? normalized
            : throw new CoachValidationException($"{fieldName} je povinné.");
    }

    private static string NormalizeBirthNumber(string? value)
    {
        var normalized = (value ?? string.Empty)
            .Replace("/", string.Empty)
            .Replace(" ", string.Empty);
        if ((normalized.Length is not 9 and not 10) || normalized.Any(c => !char.IsDigit(c)))
            throw new CoachValidationException("Rodné číslo musí obsahovat 9 nebo 10 číslic.");
        return normalized;
    }

    private void UpdateUser(SportSys.Database.Models.identity.User user, UserDto dto)
    {
        user.DisplayName = NullIfWhiteSpace(dto.DisplayName);
        user.Email = NullIfWhiteSpace(dto.Email);
        user.NormalizedEmail = user.Email == null ? null : _normalizer.NormalizeEmail(user.Email);
        user.PhoneNumber = NullIfWhiteSpace(dto.PhoneNumber);
    }

    private void UpdateUserForPromotion(
        SportSys.Database.Models.identity.User user,
        UserDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.DisplayName))
            user.DisplayName = dto.DisplayName.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            user.Email = dto.Email.Trim();
            user.NormalizedEmail = _normalizer.NormalizeEmail(user.Email);
        }
        if (!string.IsNullOrWhiteSpace(dto.PhoneNumber))
            user.PhoneNumber = dto.PhoneNumber.Trim();
    }

    private async Task EnsureUniqueBasicDataAsync(
        string personalNumber,
        string birthNumber,
        int? excludedCoachId,
        CancellationToken ct)
    {
        if (await _db.Coaches.AnyAsync(
                c => c.Id != excludedCoachId && c.PersonalNumber == personalNumber, ct))
            throw new CoachValidationException("Osobní číslo již používá jiný trenér.");

        if (await _db.Coaches.AnyAsync(
                c => c.Id != excludedCoachId && c.IdentificationNumber == birthNumber, ct))
            throw new CoachValidationException("Rodné číslo již používá jiný trenér.");
    }

    private async Task<DbCoach> GetCoachAsync(int coachId, CancellationToken ct)
    {
        return await _db.Coaches.FindAsync([coachId], ct)
            ?? throw new CoachValidationException("Trenér nebyl nalezen.");
    }

    private async Task EnsureCoachExistsAsync(int coachId, CancellationToken ct)
    {
        if (!await _db.Coaches.AnyAsync(c => c.Id == coachId, ct))
            throw new CoachValidationException("Trenér nebyl nalezen.");
    }

    private async Task EnsureLicenseTypeExistsAsync(int licenseTypeId, CancellationToken ct)
    {
        if (!await _db.CoachLicenseTypes.AnyAsync(x => x.Id == licenseTypeId && x.IsActive, ct))
            throw new CoachValidationException("Vybraný typ licence nebyl nalezen nebo není aktivní.");
    }

    private Task<bool> HasSettingOverlapAsync(
        int coachId,
        DateOnly validFrom,
        DateOnly? validTo,
        int? excludedId,
        CancellationToken ct)
    {
        return _db.CoachSettings.AnyAsync(
            x => x.CoachId == coachId &&
                 x.Id != excludedId &&
                 x.ValidFrom <= (validTo ?? DateOnly.MaxValue) &&
                 (x.ValidTo == null || x.ValidTo >= validFrom),
            ct);
    }

    private Task<bool> HasLicenseOverlapAsync(
        int coachId,
        int licenseTypeId,
        DateOnly validFrom,
        DateOnly? validTo,
        int? excludedId,
        CancellationToken ct)
    {
        return _db.CoachLicenses.AnyAsync(
            x => x.CoachId == coachId &&
                 x.CoachLicenseTypeId == licenseTypeId &&
                 x.Id != excludedId &&
                 x.ValidFrom <= (validTo ?? DateOnly.MaxValue) &&
                 (x.ValidTo == null || x.ValidTo >= validFrom),
            ct);
    }

    private static void NormalizeSetting(CoachSettingDto dto)
    {
        dto.BankAccountPrefix = NullIfWhiteSpace(dto.BankAccountPrefix);
        dto.BankAccountNumber = dto.BankAccountNumber.Trim();
        dto.BankCode = dto.BankCode.Trim();
        dto.Street = dto.Street.Trim();
        dto.City = dto.City.Trim();
        dto.ZipCode = dto.ZipCode.Trim();
        dto.HealthInsuranceCode = dto.HealthInsuranceCode.Trim();
    }

    private static void CopySetting(CoachSettingDto source, DbCoachSetting target)
    {
        target.CoachId = source.CoachId;
        target.ValidFrom = source.ValidFrom;
        target.ValidTo = source.ValidTo;
        target.BankAccountPrefix = source.BankAccountPrefix;
        target.BankAccountNumber = source.BankAccountNumber;
        target.BankCode = source.BankCode;
        target.Street = source.Street;
        target.City = source.City;
        target.ZipCode = source.ZipCode;
        target.HealthInsuranceCode = source.HealthInsuranceCode;
    }

    private static string ValidatePhoto(byte[] content, string contentType)
    {
        if (content is not { Length: > 0 })
            throw new CoachValidationException("Fotografie je prázdná.");
        if (content.Length > MaxPhotoSizeBytes)
            throw new CoachValidationException("Fotografie nesmí být větší než 5 MiB.");

        var normalized = contentType?.Trim().ToLowerInvariant();
        var signatureMatches = normalized switch
        {
            "image/jpeg" => content.Length >= 3 &&
                            content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            "image/png" => content.Length >= 8 &&
                           content.AsSpan(0, 8).SequenceEqual(
                               new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
            "image/webp" => content.Length >= 12 &&
                            content.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
                            content.AsSpan(8, 4).SequenceEqual("WEBP"u8),
            _ => false,
        };

        if (!signatureMatches)
            throw new CoachValidationException("Fotografie musí být platný soubor JPEG, PNG nebo WebP.");

        return normalized!;
    }

    private async Task SaveChangesAsync(string conflictMessage, CancellationToken ct)
    {
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException exception)
        {
            throw new CoachValidationException(conflictMessage, exception);
        }
    }

    private static string GetCoachDuplicateMessage(SqlException exception)
    {
        if (exception.Message.Contains("UX_Coach_PersonalNumber", StringComparison.Ordinal))
            return "Osobní číslo již používá jiný trenér.";
        if (exception.Message.Contains("UX_Coach_BirthNumber", StringComparison.Ordinal))
            return "Rodné číslo již používá jiný trenér.";
        if (exception.Message.Contains("PK_Coach", StringComparison.Ordinal))
            return "Vybraný uživatel již je trenérem.";

        return "Trenérský profil se nepodařilo vytvořit kvůli konfliktu uložených údajů.";
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
