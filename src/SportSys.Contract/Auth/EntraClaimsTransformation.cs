using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SportSys.Database.Context;
using SportSys.Database.Models.identity;

namespace SportSys.Contract.Auth;

/// <summary>
/// Claims transformation pro Microsoft Entra ID login flow.
/// Zpracovává pouze přihlášení přes Entra (přítomnost claim "oid").
/// Automaticky vytvoří / aktualizuje ApplicationUser a doplní business permissions.
/// </summary>
public class EntraClaimsTransformation : IClaimsTransformation
{
  private readonly SportSysDbContext _db;
  private readonly UserManager<User> _userManager;
  private readonly ILogger<EntraClaimsTransformation> _logger;

  public EntraClaimsTransformation(
      SportSysDbContext db,
      UserManager<User> userManager,
      ILogger<EntraClaimsTransformation> logger)
  {
    _db = db;
    _userManager = userManager;
    _logger = logger;
  }

  public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    // Zpracovat pouze Entra tokeny (musí být přítomny oid + tid)
    var oid = principal.FindFirstValue("oid") ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier");
    var tid = principal.FindFirstValue("tid") ?? principal.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");

    if (string.IsNullOrEmpty(oid) || string.IsNullOrEmpty(tid))
      return principal;

    // Najít nebo vytvořit ApplicationUser
    var user = await _db.Users
        .AsNoTracking()
        .FirstOrDefaultAsync(u => u.EntraOid == oid && u.EntraTenantId == tid);

    if (user is null)
    {
      user = await ProvisionEntraUserAsync(principal, oid, tid);
      if (user is null) return principal;
    }
    else
    {
      await SyncUserFieldsAsync(user, principal);
    }

    var userId = user.Id.ToString(CultureInfo.InvariantCulture);
    if (!principal.HasClaim(SportSysClaimTypes.UserId, userId))
    {
      var userIdIdentity = new ClaimsIdentity();
      userIdIdentity.AddClaim(new Claim(SportSysClaimTypes.UserId, userId));
      principal.AddIdentity(userIdIdentity);
    }
    /*
    // Načíst business permissions a role z DB
    var permissionsAndRoles = await _db.UserBusinessRoles
        .AsNoTracking()
        .Where(ubr => ubr.UserId == user.Id)
        .SelectMany(ubr => ubr.BusinessRole.Permissions
            .Select(brp => new { RoleCode = ubr.BusinessRole.Code, PermCode = brp.Permission.Code }))
        .ToListAsync();

    if (permissionsAndRoles.Count == 0)
      return principal;

    // Vytvořit novou identitu s doplněnými claims
    var identity = new ClaimsIdentity();

    foreach (var businessRole in permissionsAndRoles.Select(p => p.RoleCode).Distinct())
      identity.AddClaim(new Claim(PermissionClaimTypes.BusinessRole, businessRole));
    
    foreach (var permission in permissionsAndRoles.Select(p => p.PermCode).Distinct())
      identity.AddClaim(new Claim(PermissionClaimTypes.Permission, permission));
    */
    return principal;
  }

  private async Task<User?> ProvisionEntraUserAsync(
      ClaimsPrincipal principal, string oid, string tid)
  {
    var email = principal.FindFirstValue("preferred_username")
             ?? principal.FindFirstValue(ClaimTypes.Email)
             ?? principal.FindFirstValue("email");

    var displayName = principal.FindFirstValue("name")
                   ?? principal.FindFirstValue(ClaimTypes.Name)
                   ?? email;

    var user = new User
    {
      UserName = email ?? oid,
      Email = email,
      EntraOid = oid,
      EntraTenantId = tid,
      DisplayName = displayName,
      IsLocalAccount = false,
      LastLoginUtc = DateTime.UtcNow,
      EmailConfirmed = true,
    };

    var result = await _userManager.CreateAsync(user);
    if (!result.Succeeded)
    {
      _logger.LogWarning("Auto-provisioning Entra uživatele {Oid} selhalo: {Errors}",
          oid, string.Join(", ", result.Errors.Select(e => e.Description)));
      return null;
    }

    _logger.LogInformation("Auto-provisioning: vytvořen uživatel {UserId} pro Entra OID {Oid}", user.Id, oid);
    return user;
  }

  private async Task SyncUserFieldsAsync(User user, ClaimsPrincipal principal)
  {
    var displayName = principal.FindFirstValue("name")
                   ?? principal.FindFirstValue(ClaimTypes.Name);
    var email = principal.FindFirstValue("preferred_username")
             ?? principal.FindFirstValue(ClaimTypes.Email);

    // Aktualizovat pouze pokud se data změnila
    var needsUpdate = (displayName is not null && displayName != user.DisplayName)
                   || (email is not null && email != user.Email)
                   || user.LastLoginUtc < DateTime.UtcNow.AddMinutes(-5);

    if (!needsUpdate) return;

    await _db.Users
        .Where(u => u.Id == user.Id)
        .ExecuteUpdateAsync(s => s
            .SetProperty(u => u.DisplayName, displayName ?? user.DisplayName)
            .SetProperty(u => u.Email, email ?? user.Email)
            .SetProperty(u => u.LastLoginUtc, DateTime.UtcNow));
  }
}
