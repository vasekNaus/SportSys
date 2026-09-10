using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;
using SportSys.Contract.Auth;
using SportSys.Contract.Models.hr;
using SportSys.Contract.Services;

namespace SportSys.Razor.Areas.hr.Pages.Attendance;

public class IndexModel : PageModel
{
    private readonly CoachAttendanceService _service;

    public IndexModel(CoachAttendanceService service)
    {
        _service = service;
    }

    [BindProperty(SupportsGet = true)]
    public CoachAttendanceFilter Filter { get; set; } = new();

    [BindProperty]
    public CoachAttendanceUploadDto Upload { get; set; } = new()
    {
        PeriodYear = DateTime.Today.Year,
        PeriodMonth = DateTime.Today.Month,
    };

    [BindProperty]
    public IFormFile? AttendanceFile { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public List<CoachAttendanceListItem> Attendances { get; set; } = [];
    public List<SelectListItem> UploadCoachSelectList { get; set; } = [];
    public List<SelectListItem> FilterCoachSelectList { get; set; } = [];
    public List<SelectListItem> MonthSelectList { get; } =
    [
        .. Enumerable.Range(1, 12).Select(month =>
            new SelectListItem(
                new DateTime(2000, month, 1).ToString("MMMM"),
                month.ToString())),
    ];

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadPageAsync(ModelState.IsValid, ct);
    }

    public async Task<IActionResult> OnPostUploadAsync(CancellationToken ct)
    {
        RetainUploadModelState();

        if (AttendanceFile is null || AttendanceFile.Length == 0)
            ModelState.AddModelError(nameof(AttendanceFile), "Vyberte soubor XLSX.");
        else if (AttendanceFile.Length > CoachAttendanceXlsxValidator.MaxFileSizeBytes)
            ModelState.AddModelError(nameof(AttendanceFile), "Soubor může mít maximálně 10 MiB.");

        if (!ModelState.IsValid)
        {
            ResetInvalidFilter();
            await LoadPageAsync(loadAttendances: true, ct);
            return Page();
        }

        try
        {
            await using var stream = AttendanceFile!.OpenReadStream();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer, ct);

            var userUploadId = CurrentUserIdResolver.GetRequiredUserId(User);
            await _service.UploadAsync(
                Upload,
                buffer.ToArray(),
                AttendanceFile.ContentType,
                AttendanceFile.FileName,
                userUploadId,
                ct);

            StatusMessage = "Docházka byla nahrána.";
            return RedirectToPage(new
            {
                Filter.CoachId,
                Filter.PeriodFromYear,
                Filter.PeriodFromMonth,
                Filter.PeriodToYear,
                Filter.PeriodToMonth,
            });
        }
        catch (CoachValidationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            ResetInvalidFilter();
            await LoadPageAsync(loadAttendances: true, ct);
            return Page();
        }
    }

    public async Task<IActionResult> OnGetDownloadAsync(int id, CancellationToken ct)
    {
        var file = await _service.GetFileAsync(id, ct);
        if (file is null)
            return NotFound();

        Response.Headers.CacheControl = "private, no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }

    private async Task LoadPageAsync(bool loadAttendances, CancellationToken ct)
    {
        var coaches = await _service.GetCoachesAsync(ct);
        UploadCoachSelectList =
        [
            new("— vyberte trenéra —", ""),
            .. coaches.Select(coach => new SelectListItem(
                $"{coach.DisplayName} ({coach.PersonalNumber})",
                coach.Id.ToString())),
        ];
        FilterCoachSelectList =
        [
            new("— všichni trenéři —", ""),
            .. coaches.Select(coach => new SelectListItem(
                $"{coach.DisplayName} ({coach.PersonalNumber})",
                coach.Id.ToString())),
        ];

        if (loadAttendances)
            Attendances = await _service.GetAllAsync(Filter, ct);
    }

    private void RetainUploadModelState()
    {
        foreach (var key in ModelState.Keys
                     .Where(key =>
                         !key.Equals(nameof(Upload), StringComparison.Ordinal) &&
                         !key.StartsWith($"{nameof(Upload)}.", StringComparison.Ordinal) &&
                         !key.Equals(nameof(AttendanceFile), StringComparison.Ordinal))
                     .ToList())
        {
            ModelState.Remove(key);
        }
    }

    private void ResetInvalidFilter()
    {
        var validationResults = new List<ValidationResult>();
        if (!Validator.TryValidateObject(
                Filter,
                new ValidationContext(Filter),
                validationResults,
                validateAllProperties: true))
        {
            Filter = new CoachAttendanceFilter();
        }
    }
}
