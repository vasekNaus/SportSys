using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using SportSys.Contract.Models;
using SportSys.Razor.Models.TrainingSchedule;
using SportSys.Razor.ViewComponents;

namespace SportSys.Razor.Tests;

public class TrainingPlanProblemBuilderTests
{
    [Fact]
    public void Build_DetectsCoachConflictByIdAndReturnsTimeIntersection()
    {
        var first = Plan(
            1,
            "U12",
            new TimeOnly(14, 0),
            new TimeOnly(15, 15),
            coaches: [Coach(7, "Jan Novák", roleId: 1)]);
        var second = Plan(
            2,
            "U14",
            new TimeOnly(14, 30),
            new TimeOnly(15, 30),
            coaches: [Coach(7, "J. Novák", roleId: 2)],
            categoryName: "Starší žáci");

        var problem = Assert.Single(TrainingPlanProblemBuilder.Build([first, second]));

        Assert.Equal(TrainingPlanProblemSeverity.Error, problem.Severity);
        Assert.Equal(TrainingPlanProblemType.CoachConflict, problem.Type);
        Assert.Equal("Jan Novák", problem.CoachName);
        Assert.Equal("Mladší žáci (U12) / Starší žáci (U14)", problem.CategoryText);
        Assert.Equal(new TimeOnly(14, 30), problem.TimeFrom);
        Assert.Equal(new TimeOnly(15, 15), problem.TimeTo);
        Assert.Equal("Pondělí, 14:30–15:15", problem.TimeText);
    }

    [Theory]
    [InlineData(true, true, TrainingPlanProblemSeverity.Error)]
    [InlineData(false, false, TrainingPlanProblemSeverity.Warning)]
    [InlineData(true, false, TrainingPlanProblemSeverity.Error)]
    public void Build_ClassifiesCategoryConflictsByTrainingType(
        bool firstIsIce,
        bool secondIsIce,
        TrainingPlanProblemSeverity expectedSeverity)
    {
        var first = Plan(1, "U12", isIceTraining: firstIsIce);
        var second = Plan(
            2,
            "U12",
            new TimeOnly(14, 15),
            new TimeOnly(15, 15),
            isIceTraining: secondIsIce);

        var problem = Assert.Single(TrainingPlanProblemBuilder.Build([first, second]));

        Assert.Equal(TrainingPlanProblemType.CategoryConflict, problem.Type);
        Assert.Equal(expectedSeverity, problem.Severity);
        Assert.Null(problem.CoachName);
        Assert.Equal("Mladší žáci (U12)", problem.CategoryText);
    }

    [Fact]
    public void Build_DoesNotReportTouchingTimeIntervals()
    {
        var problems = TrainingPlanProblemBuilder.Build(
        [
            Plan(1, "U12", new TimeOnly(14, 0), new TimeOnly(15, 0), coaches: [Coach(7)]),
            Plan(2, "U14", new TimeOnly(15, 0), new TimeOnly(16, 0), coaches: [Coach(7)]),
        ]);

        Assert.Empty(problems);
    }

    [Fact]
    public void Build_RequiresOverlappingValidityAndSameWeekday()
    {
        var monday = Plan(
            1,
            "U12",
            coaches: [Coach(7)],
            from: new DateOnly(2026, 9, 7),
            to: new DateOnly(2026, 9, 13));
        var separatedValidity = Plan(
            2,
            "U14",
            coaches: [Coach(7)],
            from: new DateOnly(2026, 9, 14),
            to: new DateOnly(2026, 9, 20));
        var differentWeekday = Plan(
            3,
            "U14",
            coaches: [Coach(7)],
            day: DayOfWeek.Tuesday);

        Assert.Empty(TrainingPlanProblemBuilder.Build([monday, separatedValidity]));
        Assert.Empty(TrainingPlanProblemBuilder.Build([monday, differentWeekday]));
    }

    [Fact]
    public void Build_SkipsPersistedGroupsButNotVisualizationGroups()
    {
        var groupId = Guid.NewGuid();
        var grouped = TrainingPlanProblemBuilder.Build(
        [
            Plan(1, "U12", groupId: groupId, coaches: [Coach(7)]),
            Plan(2, "U14", groupId: groupId, coaches: [Coach(7)]),
        ]);
        var visualizationOnly = TrainingPlanProblemBuilder.Build(
        [
            Plan(1, "U12", visualizationGroupId: groupId, coaches: [Coach(7)]),
            Plan(2, "U14", visualizationGroupId: groupId, coaches: [Coach(7)]),
        ]);

        Assert.Empty(grouped);
        Assert.Equal(1, Assert.Single(visualizationOnly).FirstPlanId);
        Assert.Equal(2, Assert.Single(visualizationOnly).SecondPlanId);
    }

    [Fact]
    public void Build_DeduplicatesCoachRolesAndReportsEveryConflictingPair()
    {
        var plans = new[]
        {
            Plan(
                3,
                "U16",
                coaches: [Coach(7, roleId: 1), Coach(7, roleId: 2)]),
            Plan(1, "U12", coaches: [Coach(7)]),
            Plan(2, "U14", coaches: [Coach(7)]),
        };

        var problems = TrainingPlanProblemBuilder.Build(plans)
            .Where(problem => problem.Type == TrainingPlanProblemType.CoachConflict)
            .ToList();

        Assert.Equal(3, problems.Count);
        Assert.Equal(
            new[] { (1, 2), (1, 3), (2, 3) },
            problems.Select(problem => (problem.FirstPlanId, problem.SecondPlanId)));
    }

    [Fact]
    public void Build_UsesActualWeekdayOccurrenceWithinValidityIntersection()
    {
        var first = Plan(
            1,
            "U12",
            from: new DateOnly(2026, 9, 7),
            to: new DateOnly(2026, 9, 8),
            coaches: [Coach(7)]);
        var second = Plan(
            2,
            "U14",
            from: new DateOnly(2026, 9, 8),
            to: new DateOnly(2026, 9, 8),
            coaches: [Coach(7)]);

        Assert.Empty(TrainingPlanProblemBuilder.Build([first, second]));
    }

    [Fact]
    public void Build_RespectsPlansFilteredBySelectedDay()
    {
        var plans = new[]
        {
            Plan(1, "U12", coaches: [Coach(7)], day: DayOfWeek.Monday),
            Plan(2, "U14", coaches: [Coach(7)], day: DayOfWeek.Tuesday),
        };

        var filtered = WeekDayNames.FilterByDay(
            plans,
            plan => plan.DayOfWeek,
            [DayOfWeek.Monday]);

        Assert.Empty(TrainingPlanProblemBuilder.Build(filtered));
    }

    [Fact]
    public async Task DefaultView_RendersProblemsAndEncodesPlanTitles()
    {
        var problem = Assert.Single(TrainingPlanProblemBuilder.Build(
        [
            Plan(1, "U12", title: "Led <script>", coaches: [Coach(7)]),
            Plan(2, "U14", coaches: [Coach(7)]),
        ]));
        var html = await RenderViewAsync([problem]);
        var decodedHtml = WebUtility.HtmlDecode(html);

        Assert.Contains("<h2>Přehled problémových bloků</h2>", decodedHtml);
        Assert.Contains("<th>Závažnost</th>", decodedHtml);
        Assert.Contains("<th>Trénink 2</th>", decodedHtml);
        Assert.Contains("training-plan-problem__severity--error", html);
        Assert.Contains("fa-circle-exclamation", html);
        Assert.Contains("Jan Novák", decodedHtml);
        Assert.Contains("Led &lt;script&gt;", html);
        Assert.DoesNotContain("<script>", html);
        Assert.Contains("Pondělí, 14:00–15:00", decodedHtml);
    }

    [Fact]
    public async Task DefaultView_RendersEmptyState()
    {
        var html = WebUtility.HtmlDecode(await RenderViewAsync([]));

        Assert.Contains("nebyly nalezeny žádné problémové bloky", html);
        Assert.DoesNotContain("<table", html);
    }

    private static async Task<string> RenderViewAsync(
        IReadOnlyList<TrainingPlanProblem> problems)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(TrainingPlanProblemsViewComponent).Assembly.GetName().Name,
        });
        builder.Services.AddRazorPages();
        await using var app = builder.Build();
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var actionContext = new ActionContext(
            new DefaultHttpContext { RequestServices = services },
            new RouteData(),
            new ActionDescriptor());
        actionContext.HttpContext.SetEndpoint(new Endpoint(
            _ => Task.CompletedTask,
            new EndpointMetadataCollection(),
            "Training plan problems test"));
        var viewResult = services.GetRequiredService<IRazorViewEngine>().GetView(
            null,
            "/Pages/Shared/Components/TrainingPlanProblems/Default.cshtml",
            isMainPage: false);
        Assert.True(viewResult.Success);

        var viewData = new ViewDataDictionary<IReadOnlyList<TrainingPlanProblem>>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = problems,
        };
        var tempData = new TempDataDictionary(
            actionContext.HttpContext,
            services.GetRequiredService<ITempDataProvider>());
        using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            viewResult.View,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions());

        await viewResult.View.RenderAsync(viewContext);
        return writer.ToString();
    }

    private static TrainingPlanScheduleItemDto Plan(
        int id,
        string categoryCode,
        TimeOnly? timeFrom = null,
        TimeOnly? timeTo = null,
        bool isIceTraining = true,
        Guid? groupId = null,
        Guid? visualizationGroupId = null,
        IReadOnlyList<TrainingPlanCoachDto>? coaches = null,
        string title = "",
        string? categoryName = null,
        DateOnly? from = null,
        DateOnly? to = null,
        DayOfWeek day = DayOfWeek.Monday)
        => new()
        {
            Id = id,
            From = from ?? new DateOnly(2026, 9, 1),
            To = to ?? new DateOnly(2026, 9, 30),
            DayName = day.ToString(),
            Title = title,
            TimeFrom = timeFrom ?? new TimeOnly(14, 0),
            TimeTo = timeTo ?? new TimeOnly(15, 0),
            GroupId = groupId,
            VisualizationGroupId = visualizationGroupId,
            SeasonCategoryCode = categoryCode,
            SeasonCategoryName = categoryName ?? "Mladší žáci",
            TrainingTypeName = isIceTraining ? "Led" : "Suchá příprava",
            IsIceTraining = isIceTraining,
            LocationName = "Zimní stadion",
            CoachAssignments = coaches ?? [],
        };

    private static TrainingPlanCoachDto Coach(
        int id,
        string name = "Jan Novák",
        int roleId = 1)
        => new()
        {
            CoachId = id,
            FullName = name,
            LastName = name[(name.LastIndexOf(' ') + 1)..],
            CoachRoleId = roleId,
            CoachRoleCode = roleId == 1 ? "H" : "A",
            CoachRoleName = roleId == 1 ? "Hlavní trenér" : "Asistent",
            CoachRoleOrder = roleId,
        };
}
