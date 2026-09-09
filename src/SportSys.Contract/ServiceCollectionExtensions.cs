using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SportSys.Contract.Auth;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Database.Models.identity;

namespace SportSys.Contract;

public static class ServiceCollectionExtensions
{
  /// <summary>
  /// Registruje SportSysDbContext, ASP.NET Core Identity, Claims transformation a všechny
  /// aplikační servisy z SportSys.Contract.
  /// Volat z Program.cs webové aplikace – Razor projekt nesmí DbContext registrovat přímo.
  /// </summary>
  public static IServiceCollection AddSportSysServices(
      this IServiceCollection services,
      IConfiguration configuration)
  {
    services.AddDbContext<SportSysDbContext>(options =>
        options.UseSqlServer(
            configuration.GetConnectionString("DefaultConnection"),
            sql => sql.UseNetTopologySuite()));

    // Identity – lokální uložení uživatelů, rolí a claims.
    // AddIdentityCore (ne AddIdentity) zachovává OIDC jako výchozí auth scheme.
    // AddSignInManager přidá SignInManager potřebný pro lokální přihlašování.
    services.AddIdentityCore<User>()
        .AddRoles<Role>()
        .AddSignInManager()
        .AddEntityFrameworkStores<SportSysDbContext>()
        .AddDefaultTokenProviders();

    // Cookie schémata pro Identity (lokální přihlašování, external login callback)
    services.AddAuthentication()
        .AddCookie(IdentityConstants.ApplicationScheme)
        .AddCookie(IdentityConstants.ExternalScheme)
        .AddCookie(IdentityConstants.TwoFactorUserIdScheme);

    // Entra login flow: automatický provisioning + sync + business permissions
    services.AddScoped<IClaimsTransformation, EntraClaimsTransformation>();

    // Business authorization policies
    services.AddAuthorization(options =>
    {
      // Výchozí politika: všechny požadavky musí být autorizovány
      options.FallbackPolicy = options.DefaultPolicy;

      // Systémové Identity role
      options.AddPolicy("SystemAdmin", p => p.RequireRole("SystemAdmin"));
      options.AddPolicy("Support", p => p.RequireRole("Support"));
      options.AddPolicy("InternalUser", p => p.RequireRole("InternalUser"));

      // Business permissions – doplňovat dle Permission.Code záznamů v DB
      // Příklad: options.AddPolicy("invoice.approve",
      //     p => p.RequireClaim(PermissionClaimTypes.Permission, "invoice.approve"));
    });

    services.AddScoped<CsvMatchImportService>();
    services.AddScoped<IceRinkService>();
    services.AddScoped<TeamService>();
    services.AddScoped<SeasonService>();
    services.AddScoped<SeasonCategoryService>();
    services.AddScoped<TrainingScheduleService>();
    services.AddScoped<TrainingService>();
    services.AddScoped<TrainingRequirementService>();
    services.AddScoped<CoachService>();
    services.AddScoped<CoachAttendanceService>();

    services.AddScoped<ManufacturerService>();
    services.AddScoped<LocationService>();
    services.AddScoped<LoanService>();
    services.AddScoped<CategoryService>();
    services.AddScoped<ItemKindService>();
    services.AddScoped<InventoryItemService>();

    return services;
  }
}
