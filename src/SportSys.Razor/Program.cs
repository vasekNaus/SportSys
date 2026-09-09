using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Localization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Serilog;
using SportSys.Contract;
using SportSys.Razor.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);
var defaultCulture = CultureInfo.GetCultureInfo("cs-CZ");

CultureInfo.DefaultThreadCurrentCulture = defaultCulture;
CultureInfo.DefaultThreadCurrentUICulture = defaultCulture;

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Add services to the container.
builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddRazorPages(options =>
    options.Conventions.AuthorizeAreaFolder("hr", "/Coach", "SystemAdmin"))
    .AddMicrosoftIdentityUI();

// Registrace SportSysDbContext, Identity, Claims transformation, Contract servisů a authorization policies
builder.Services.AddSportSysServices(builder.Configuration);
builder.Services.AddSingleton<TrainingScheduleExcelExporter>();

var app = builder.Build();

app.UseSerilogRequestLogging();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture(defaultCulture.Name)
    .AddSupportedCultures(defaultCulture.Name)
    .AddSupportedUICultures(defaultCulture.Name));

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

app.Run();
