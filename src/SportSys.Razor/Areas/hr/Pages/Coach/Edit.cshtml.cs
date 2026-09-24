using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using SportSys.Contract.Models.hr;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.hr.Pages.Coach;

public class EditModel : PageModel
{
    private const long MaxPhotoSizeBytes = 5 * 1024 * 1024;
    private readonly CoachService _service;

    public EditModel(CoachService service)
    {
        _service = service;
    }

    [BindProperty]
    public CoachDetailDto BasicInput { get; set; } = new();

    [BindProperty]
    [Range(1, int.MaxValue, ErrorMessage = "Uživatel je povinný.")]
    [Display(Name = "Uživatel")]
    public int? SelectedUserId { get; set; }

    [BindProperty]
    public CoachContractDto ContractInput { get; set; } = new() { IsActive = true };

    [BindProperty]
    public CoachSettingDto SettingInput { get; set; } = new() { ValidFrom = DateOnly.FromDateTime(DateTime.Today) };

    [BindProperty]
    public CoachLicenseDto LicenseInput { get; set; } = new() { ValidFrom = DateOnly.FromDateTime(DateTime.Today) };

    [BindProperty]
    public IFormFile? Photo { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public CoachDetailDto Coach { get; set; } = new();
    public List<SelectListItem> UserSelectList { get; set; } = [];
    public List<SelectListItem> SeasonSelectList { get; set; } = [];
    public List<SelectListItem> LicenseTypeSelectList { get; set; } = [];
    public List<SelectListItem> ContractTypeSelectList { get; set; } = [];
    public string ActiveTab { get; set; } = "basic";
    public bool IsNew => BasicInput.Id == 0;

    public async Task<IActionResult> OnGetAsync(
        int? id,
        string? activeTab,
        int? contractId,
        int? settingId,
        int? licenseId,
        CancellationToken ct)
    {
        ActiveTab = NormalizeTab(activeTab);

        if (id.HasValue)
        {
            var coach = await _service.GetByIdAsync(id.Value, ct);
            if (coach is null)
                return NotFound();

            Coach = coach;
            BasicInput = coach;
            ContractInput = contractId.HasValue
                ? coach.Contracts.FirstOrDefault(x => x.Id == contractId.Value)
                    ?? new CoachContractDto { CoachId = id.Value, IsActive = true }
                : new CoachContractDto { CoachId = id.Value, IsActive = true };
            SettingInput = settingId.HasValue
                ? coach.Settings.FirstOrDefault(x => x.Id == settingId.Value)
                    ?? new CoachSettingDto { CoachId = id.Value, ValidFrom = DateOnly.FromDateTime(DateTime.Today) }
                : new CoachSettingDto { CoachId = id.Value, ValidFrom = DateOnly.FromDateTime(DateTime.Today) };
            LicenseInput = licenseId.HasValue
                ? coach.Licenses.FirstOrDefault(x => x.Id == licenseId.Value)
                    ?? new CoachLicenseDto { CoachId = id.Value, ValidFrom = DateOnly.FromDateTime(DateTime.Today) }
                : new CoachLicenseDto { CoachId = id.Value, ValidFrom = DateOnly.FromDateTime(DateTime.Today) };
        }

        await LoadSelectListsAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnGetPhotoAsync(int id, CancellationToken ct)
    {
        var photo = await _service.GetPhotoAsync(id, ct);
        if (photo is null)
            return NotFound();

        Response.Headers.CacheControl = "private, max-age=300";
        return File(photo.Content, photo.ContentType);
    }

    public async Task<IActionResult> OnPostSaveBasicAsync(CancellationToken ct)
    {
        RetainModelState(nameof(BasicInput));
        if (IsNew && SelectedUserId.GetValueOrDefault() <= 0)
            ModelState.AddModelError(nameof(SelectedUserId), "Uživatel je povinný.");

        if (!ModelState.IsValid)
            return await ReloadPageAsync(BasicInput.Id, "basic", ct, preserveBasicInput: true);

        try
        {
            var coachId = IsNew
                ? await _service.CreateAsync(SelectedUserId!.Value, BasicInput, ct)
                : BasicInput.Id;

            if (!IsNew)
                await _service.UpdateBasicAsync(BasicInput, ct);

            TempData["StatusMessage"] = "Základní údaje trenéra byly uloženy.";
            return RedirectToPage(new { id = coachId, activeTab = "basic" });
        }
        catch (CoachValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadPageAsync(BasicInput.Id, "basic", ct, preserveBasicInput: true);
        }
    }

    public async Task<IActionResult> OnPostSavePhotoAsync(int id, CancellationToken ct)
    {
        RetainModelState(nameof(Photo));
        if (Photo is null || Photo.Length == 0)
        {
            ModelState.AddModelError(nameof(Photo), "Vyberte fotografii.");
            return await ReloadPageAsync(id, "basic", ct);
        }

        try
        {
            if (Photo.Length > MaxPhotoSizeBytes)
                throw new CoachValidationException("Fotografie může mít maximálně 5 MB.");

            await using var stream = Photo.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);
            await _service.SetPhotoAsync(id, buffer.ToArray(), Photo.ContentType, Photo.FileName, ct);

            TempData["StatusMessage"] = "Fotografie byla uložena.";
            return RedirectToPage(new { id, activeTab = "basic" });
        }
        catch (CoachValidationException ex)
        {
            ModelState.AddModelError(nameof(Photo), ex.Message);
            return await ReloadPageAsync(id, "basic", ct);
        }
    }

    public async Task<IActionResult> OnPostDeletePhotoAsync(int id, CancellationToken ct)
    {
        await _service.DeletePhotoAsync(id, ct);
        TempData["StatusMessage"] = "Fotografie byla odstraněna.";
        return RedirectToPage(new { id, activeTab = "basic" });
    }

    public async Task<IActionResult> OnPostSaveContractAsync(CancellationToken ct)
    {
        RetainModelState(nameof(ContractInput));
        if (!ModelState.IsValid)
            return await ReloadPageAsync(ContractInput.CoachId, "contracts", ct);

        try
        {
            if (ContractInput.Id == 0)
                await _service.CreateContractAsync(ContractInput, ct);
            else
                await _service.UpdateContractAsync(ContractInput, ct);

            TempData["StatusMessage"] = "Smlouva byla uložena.";
            return RedirectToPage(new { id = ContractInput.CoachId, activeTab = "contracts" });
        }
        catch (CoachValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadPageAsync(ContractInput.CoachId, "contracts", ct);
        }
    }

    public async Task<IActionResult> OnPostSetContractActiveAsync(int coachId, int contractId, bool isActive, CancellationToken ct)
    {
        try
        {
            await _service.SetContractActiveAsync(coachId, contractId, isActive, ct);
            TempData["StatusMessage"] = isActive ? "Smlouva byla aktivována." : "Smlouva byla deaktivována.";
            return RedirectToPage(new { id = coachId, activeTab = "contracts" });
        }
        catch (CoachValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadPageAsync(coachId, "contracts", ct);
        }
    }

    public async Task<IActionResult> OnPostSaveSettingAsync(CancellationToken ct)
    {
        RetainModelState(nameof(SettingInput));
        if (!ModelState.IsValid)
            return await ReloadPageAsync(SettingInput.CoachId, "settings", ct);

        try
        {
            if (SettingInput.Id == 0)
                await _service.CreateSettingAsync(SettingInput, ct);
            else
                await _service.UpdateSettingAsync(SettingInput, ct);

            TempData["StatusMessage"] = "Personální nastavení bylo uloženo.";
            return RedirectToPage(new { id = SettingInput.CoachId, activeTab = "settings" });
        }
        catch (CoachValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadPageAsync(SettingInput.CoachId, "settings", ct);
        }
    }

    public async Task<IActionResult> OnPostSaveLicenseAsync(CancellationToken ct)
    {
        RetainModelState(nameof(LicenseInput));
        if (!ModelState.IsValid)
            return await ReloadPageAsync(LicenseInput.CoachId, "licenses", ct);

        try
        {
            if (LicenseInput.Id == 0)
                await _service.CreateLicenseAsync(LicenseInput, ct);
            else
                await _service.UpdateLicenseAsync(LicenseInput, ct);

            TempData["StatusMessage"] = "Licence byla uložena.";
            return RedirectToPage(new { id = LicenseInput.CoachId, activeTab = "licenses" });
        }
        catch (CoachValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await ReloadPageAsync(LicenseInput.CoachId, "licenses", ct);
        }
    }

    private async Task<IActionResult> ReloadPageAsync(
        int coachId,
        string activeTab,
        CancellationToken ct,
        bool preserveBasicInput = false)
    {
        ActiveTab = activeTab;
        if (coachId != 0)
        {
            Coach = await _service.GetByIdAsync(coachId, ct)
                ?? throw new CoachValidationException($"Trenér s ID {coachId} nebyl nalezen.");
            if (!preserveBasicInput)
                BasicInput = Coach;
        }

        await LoadSelectListsAsync(ct);
        return Page();
    }

    private async Task LoadSelectListsAsync(CancellationToken ct)
    {
        var users = await _service.GetAvailableUsersAsync(
            BasicInput.Id == 0 ? SelectedUserId : BasicInput.Id,
            ct);
        UserSelectList =
        [
            new("— vyberte uživatele —", ""),
            .. users.Select(u => new SelectListItem(
                string.IsNullOrWhiteSpace(u.Email) ? u.DisplayName : $"{u.DisplayName} ({u.Email})",
                u.Id.ToString())),
        ];

        var seasons = await _service.GetSeasonsAsync(ct);
        SeasonSelectList =
        [
            new("— vyberte sezonu —", ""),
            .. seasons.Select(s => new SelectListItem(s.Name, s.Id.ToString())),
        ];

        var licenseTypes = await _service.GetLicenseTypesAsync(ct);
        LicenseTypeSelectList =
        [
            new("— vyberte licenci —", ""),
            .. licenseTypes.Select(t => new SelectListItem(t.Name, t.Id.ToString())),
        ];

        ContractTypeSelectList =
        [
            new("DPP", ((byte)ECoachContractType.Dpp).ToString()),
            new("OSVČ", ((byte)ECoachContractType.SelfEmployed).ToString()),
        ];
    }

    private void RetainModelState(string prefix)
    {
        foreach (var key in ModelState.Keys
                     .Where(k => !k.Equals(prefix, StringComparison.Ordinal)
                                 && !k.StartsWith($"{prefix}.", StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }
    }

    private static string NormalizeTab(string? activeTab) =>
        activeTab is "contracts" or "settings" or "licenses" ? activeTab : "basic";
}
