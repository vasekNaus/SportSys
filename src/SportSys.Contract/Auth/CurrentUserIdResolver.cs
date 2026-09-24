using System.Globalization;
using System.Security.Claims;
using SportSys.Contract.Models.hr;

namespace SportSys.Contract.Auth;

public static class CurrentUserIdResolver
{
    public static int GetRequiredUserId(ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var localUserId = principal.FindFirstValue(SportSysClaimTypes.UserId);
        if (localUserId is not null)
        {
            return ParseRequiredUserId(
                localUserId,
                $"Claim '{SportSysClaimTypes.UserId}' neobsahuje platné lokální ID uživatele.");
        }

        var nameIdentifier = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (nameIdentifier is not null)
        {
            return ParseRequiredUserId(
                nameIdentifier,
                $"Claim '{ClaimTypes.NameIdentifier}' neobsahuje platné lokální ID uživatele.");
        }

        throw new CoachValidationException(
            "Přihlášený uživatel nemá claim s lokálním ID uživatele SportSys.");
    }

    private static int ParseRequiredUserId(string value, string errorMessage)
    {
        if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var userId)
            && userId > 0)
        {
            return userId;
        }

        throw new CoachValidationException(errorMessage);
    }
}
