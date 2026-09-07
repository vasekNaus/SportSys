using Microsoft.AspNetCore.Identity;
using SportSys.Database.Models.hr;

namespace SportSys.Database.Models.identity;

public class User : IdentityUser<int>
{
  /// <summary>Object ID (oid) uživatele v Microsoft Entra ID.</summary>
  public string? EntraOid { get; set; }

  /// <summary>Tenant ID (tid) z Entra ID tokenu.</summary>
  public string? EntraTenantId { get; set; }

  /// <summary>Zobrazované jméno synchronizované z Entra ID.</summary>
  public string? DisplayName { get; set; }

  /// <summary>True = lokální účet (ne Entra). False = synchronizovaný z Entra ID.</summary>
  public bool IsLocalAccount { get; set; }

  /// <summary>Čas posledního přihlášení (UTC).</summary>
  public DateTime? LastLoginUtc { get; set; }
}
