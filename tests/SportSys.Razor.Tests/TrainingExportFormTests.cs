using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Razor.Models;
using SportSys.Razor.Models.TrainingSchedule;
using SportSys.Razor.Services;
using PlanIndexModel = SportSys.Razor.Areas.sport.Pages.Training.Plan.IndexModel;
using RequirementIndexModel = SportSys.Razor.Areas.sport.Pages.Training.Requirement.IndexModel;

namespace SportSys.Razor.Tests;

public class TrainingExportFormTests
{
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public async Task ExportForms_SubmitHandlerAndExplicitBooleanValues(
        bool planPage,
        bool option,
        bool invalidBoolean)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(RequirementIndexModel).Assembly.GetName().Name,
        });
        builder.Services.AddRazorPages();
        await using var app = builder.Build();
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        using var db = new SportSysDbContext(new DbContextOptionsBuilder<SportSysDbContext>().Options);
        var exporter = new TrainingOverviewExcelExporter();
        PageModel model;
        if (planPage)
        {
            var plan = new PlanIndexModel(
                new TrainingScheduleService(db), new TrainingRequirementService(db), exporter)
            {
                SeasonId = 1,
                ShowEmptyRows = option,
                MergeTrainings = !option,
                SelectedCategoryCodes = ["U12", "U14"],
                SelectedDaysOfWeek = [DayOfWeek.Monday, DayOfWeek.Friday],
            };
            plan.Seasons.Add(new SeasonDto { Id = 1, Name = "2026/2027" });
            plan.SeasonCategories.Add(new SeasonCategoryDto { SeasonId = 1, Code = "U12" });
            typeof(PlanIndexModel).GetProperty(nameof(plan.ScheduleView))!.SetValue(
                plan, new TrainingScheduleViewModel([], [], allowEditing: false));
            model = plan;
        }
        else
        {
            var requirement = new RequirementIndexModel(
                new TrainingRequirementService(db),
                new MatchRequirementService(db),
                new TrainingScheduleService(db),
                exporter)
            {
                SeasonId = 1,
                ShowMatches = option,
                SelectedCategoryCodes = ["U12", "U14"],
            };
            requirement.Seasons.Add(new SeasonDto { Id = 1, Name = "2026/2027" });
            requirement.Requirements.Add(new TrainingRequirementListItem());
            model = requirement;
        }

        model.GetType().GetProperty("CoverageRows")!.SetValue(
            model,
            new List<TrainingRequirementCoverageRow>
            {
                new()
                {
                    SeasonName = "2026/2027",
                    SeasonCategoryCode = "U12",
                    SeasonCategoryName = "U12",
                    TrainingTypeName = "Led",
                    CoachName = "Test",
                    CoachRoleName = "Hlavní",
                },
            });

        var pageName = planPage ? "Plan" : "Requirement";
        var pagePath = $"/Areas/sport/Pages/Training/{pageName}/Index.cshtml";
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = services },
            new RouteData(),
            new PageActionDescriptor
            {
                ViewEnginePath = $"/Training/{pageName}/Index",
                RelativePath = pagePath,
                RouteValues = new Dictionary<string, string?>
                {
                    ["area"] = "sport",
                    ["page"] = $"/Training/{pageName}/Index",
                },
            });
        actionContext.HttpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask, new EndpointMetadataCollection(), "Export form test"));
        actionContext.RouteData.Values["area"] = "sport";
        actionContext.RouteData.Values["page"] = $"/Training/{pageName}/Index";
        var view = services.GetRequiredService<IRazorViewEngine>().GetView(
            null, pagePath, isMainPage: false);
        Assert.True(view.Success);

        async Task<string> RenderAsync(
            Microsoft.AspNetCore.Mvc.ViewEngines.IView targetView,
            ViewDataDictionary data)
        {
            using var writer = new StringWriter();
            await targetView.RenderAsync(new ViewContext(
                actionContext, targetView, data,
                new TempDataDictionary(
                    actionContext.HttpContext, services.GetRequiredService<ITempDataProvider>()),
                writer, new HtmlHelperOptions()));
            return writer.ToString();
        }

        ViewDataDictionary viewData = planPage
            ? new ViewDataDictionary<PlanIndexModel>(
                new EmptyModelMetadataProvider(), actionContext.ModelState) { Model = (PlanIndexModel)model }
            : new ViewDataDictionary<RequirementIndexModel>(
                new EmptyModelMetadataProvider(), actionContext.ModelState) { Model = (RequirementIndexModel)model };
        var page = Assert.IsAssignableFrom<Page>(Assert.IsType<RazorView>(view.View).RazorPage);
        if (invalidBoolean)
        {
            var booleanFields = planPage
                ? new[] { "ShowEmptyRows", "MergeTrainings" }
                : new[] { "ShowTrainings", "ShowMatches" };
            foreach (var field in booleanFields)
            {
                actionContext.ModelState.SetModelValue(field, new ValueProviderResult("value"));
                actionContext.ModelState.AddModelError(field, "Neplatná hodnota přepínače filtru.");
            }
        }

        page.PageContext = new PageContext(actionContext) { ViewData = viewData };
        model.PageContext = page.PageContext;
        var html = await RenderAsync(view.View, viewData);
        if (invalidBoolean)
        {
            Assert.False(model.ModelState.IsValid);
            Assert.Contains("Neplatná hodnota přepínače filtru.", WebUtility.HtmlDecode(html));
        }
        if (planPage)
        {
            var tableView = services.GetRequiredService<IRazorViewEngine>().GetView(
                null, "/Areas/sport/Pages/Training/Plan/_TrainingPlanTable.cshtml", isMainPage: false);
            Assert.True(tableView.Success);
            html += await RenderAsync(tableView.View, viewData);
        }

        var exportForms = Regex.Matches(
                html, @"<form\b[^>]*>.*?</form>", RegexOptions.Singleline)
            .Select(match => match.Value)
            .Where(form => form.Contains("name=\"handler\"", StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, exportForms.Count);
        Assert.Contains(exportForms, form =>
            form.Contains($"value=\"{(planPage ? "ExportPlans" : "ExportRequirements")}\""));
        Assert.Contains(exportForms, form => form.Contains("value=\"ExportCoverage\""));
        foreach (var form in exportForms)
        {
            var fields = Regex.Matches(
                    form, "<input\\b[^>]*name=\"([^\"]+)\"[^>]*value=\"([^\"]*)\"[^>]*>")
                .ToLookup(
                    match => match.Groups[1].Value,
                    match => WebUtility.HtmlDecode(match.Groups[2].Value));
            Assert.Equal(["U12", "U14"], fields["SelectedCategoryCodes"]);
            if (planPage)
            {
                Assert.Equal(option, bool.Parse(Assert.Single(fields["ShowEmptyRows"])));
                Assert.Equal(!option, bool.Parse(Assert.Single(fields["MergeTrainings"])));
                Assert.Equal(["Monday", "Friday"], fields["SelectedDaysOfWeek"]);
            }
            else
            {
                Assert.True(bool.Parse(Assert.Single(fields["ShowTrainings"])));
                Assert.Equal(option, bool.Parse(Assert.Single(fields["ShowMatches"])));
            }
        }
    }
}
