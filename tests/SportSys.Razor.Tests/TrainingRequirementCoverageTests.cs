using System.Globalization;
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
using SportSys.Razor.Models;
using SportSys.Razor.ViewComponents;

namespace SportSys.Razor.Tests;

public class TrainingRequirementCoverageTests
{
    [Fact]
    public void Build_AggregatesHoursAcrossPhasesAndMatchesByStableIds()
    {
        var requirements = new[]
        {
            Requirement(1.5m, phaseId: 1),
            Requirement(2.5m, phaseId: 2),
        };
        var plans = new[]
        {
            Plan(90, phaseName: "Příprava"),
            Plan(150, phaseName: "Sezóna"),
        };

        var row = Assert.Single(TrainingRequirementCoverageBuilder.Build(requirements, plans));

        Assert.Equal(4m, row.RequiredHours);
        Assert.Equal(4m, row.PlannedHours);
        Assert.Equal(TrainingRequirementCoverageStatus.Match, row.Status);
        Assert.Equal("Mladší žáci", row.SeasonCategoryName);
        Assert.Equal("Hlavní trenér", row.CoachRoleName);
    }

    [Fact]
    public void Build_IncludesRowsWithOnlyRequirementOrOnlyPlan()
    {
        var rows = TrainingRequirementCoverageBuilder.Build(
        [
            Requirement(2m),
        ],
        [
            Plan(90, coachId: 11),
        ]);

        Assert.Equal(2, rows.Count);
        Assert.Equal(
            (2m, 0m, TrainingRequirementCoverageStatus.UnderPlan),
            (rows[0].RequiredHours, rows[0].PlannedHours, rows[0].Status));
        Assert.Equal(
            (0m, 1.5m, TrainingRequirementCoverageStatus.OverPlan),
            (rows[1].RequiredHours, rows[1].PlannedHours, rows[1].Status));
        Assert.Equal("Nad plán", rows[1].StatusText);
    }

    [Fact]
    public void Build_FiltersNestedCoachAssignmentsAndKeepsRolesSeparate()
    {
        var requirement = Requirement(3m);
        requirement.CoachAssignments =
        [
            CoachAssignment(coachId: 10, roleId: 1, hours: 3m),
            CoachAssignment(coachId: 11, roleId: 1, hours: 6m),
        ];
        var plan = Plan(180);
        plan.CoachAssignments =
        [
            PlanCoach(coachId: 10, roleId: 1),
            PlanCoach(coachId: 11, roleId: 1),
            PlanCoach(coachId: 10, roleId: 2),
        ];

        var rows = TrainingRequirementCoverageBuilder.Build(
            [requirement],
            [plan],
            selectedCoachIds: [10]);

        Assert.Equal(2, rows.Count);
        Assert.All(rows, row => Assert.Equal(10, row.CoachId));
        Assert.Equal(1, rows[0].CoachRoleId);
        Assert.Equal(3m, rows[0].RequiredHours);
        Assert.Equal(3m, rows[0].PlannedHours);
        Assert.Equal(2, rows[1].CoachRoleId);
        Assert.Equal(0m, rows[1].RequiredHours);
        Assert.Equal(3m, rows[1].PlannedHours);
    }

    [Theory]
    [InlineData(29, 0.48)]
    [InlineData(30, 0.5)]
    [InlineData(31, 0.52)]
    public void Build_RoundsTotalMinutesToTwoDecimalPlacesBeforeStatus(
        int minutes,
        decimal expectedHours)
    {
        var row = Assert.Single(
            TrainingRequirementCoverageBuilder.Build(
                [Requirement(expectedHours)],
                [Plan(minutes)]));

        Assert.Equal(expectedHours, row.PlannedHours);
        Assert.Equal(TrainingRequirementCoverageStatus.Match, row.Status);
    }

    [Fact]
    public void Build_DeduplicatesRepeatedAssignmentsWithinOnePlan()
    {
        var plan = Plan(60);
        plan.CoachAssignments =
        [
            PlanCoach(coachId: 10),
            PlanCoach(coachId: 10),
        ];

        var row = Assert.Single(
            TrainingRequirementCoverageBuilder.Build([], [plan]));

        Assert.Equal(1m, row.PlannedHours);
    }

    [Fact]
    public void Build_RejectsAssignedPlanWithoutComputedDuration()
    {
        var plan = Plan(60);
        plan.DurationMinutes = null;

        var exception = Assert.Throws<InvalidOperationException>(
            () => TrainingRequirementCoverageBuilder.Build([], [plan]));

        Assert.Contains("TrainingPlan 20", exception.Message);
    }

    [Theory]
    [InlineData("6", "6")]
    [InlineData("6.5", "6,50")]
    [InlineData("7.25", "7,25")]
    public void FormatHours_UsesCzechDecimalFormatting(
        string hours,
        string expected)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal(
                expected,
                TrainingRequirementCoverageBuilder.FormatHours(
                    decimal.Parse(hours, CultureInfo.InvariantCulture)));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData(120, "Shoda", "Požadované hodiny odpovídají plánu.", "coverage-status--match", "fa-equals")]
    [InlineData(180, "Nad plán", "Plán obsahuje více hodin než požadavek.", "coverage-status--over", "fa-triangle-exclamation")]
    [InlineData(90, "Nesplňuje plán", "Plán neobsahuje dostatek hodin.", "coverage-status--under", "fa-circle-xmark")]
    public async Task DefaultView_RendersStatusTextAndEncodesData(
        int plannedMinutes,
        string expectedStatus,
        string expectedTooltip,
        string expectedClass,
        string expectedIcon)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(TrainingRequirementCoverageViewComponent).Assembly.GetName().Name,
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
            "Coverage test"));
        var viewResult = services.GetRequiredService<IRazorViewEngine>().GetView(
            null,
            "/Pages/Shared/Components/TrainingRequirementCoverage/Default.cshtml",
            isMainPage: false);
        Assert.True(viewResult.Success);

        var rows = TrainingRequirementCoverageBuilder.Build(
        [
            Requirement(2m, coachName: "Jan <script>"),
        ],
        [
            Plan(plannedMinutes),
        ]);
        var viewData = new ViewDataDictionary<IReadOnlyList<TrainingRequirementCoverageRow>>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = rows,
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

        var html = writer.ToString();
        var decodedHtml = WebUtility.HtmlDecode(html);
        Assert.Contains($">{expectedStatus}<", decodedHtml);
        Assert.Contains($"title=\"{expectedTooltip}\"", decodedHtml);
        Assert.Contains($"aria-label=\"{expectedStatus}: {expectedTooltip}\"", decodedHtml);
        Assert.Contains(expectedClass, html);
        Assert.Contains(expectedIcon, html);
        Assert.DoesNotContain(">Splňuje plán<", decodedHtml);
        Assert.DoesNotContain("fa-circle-check", html);
        Assert.Contains("Jan &lt;script&gt;", html);
        Assert.DoesNotContain("<td>Jan <script></td>", html);
    }

    private static TrainingRequirementListItem Requirement(
        decimal hours,
        int phaseId = 1,
        int coachId = 10,
        string coachName = "Jan Novák")
        => new()
        {
            Id = phaseId,
            SeasonId = 1,
            SeasonName = "2026/2027",
            SeasonCategoryCode = "U12",
            SeasonCategoryName = "Mladší žáci",
            SeasonCategoryOrder = 2,
            TrainingTypeId = 3,
            TrainingTypeName = "Led",
            TrainingPhaseId = phaseId,
            TrainingPhaseName = phaseId == 1 ? "Příprava" : "Sezóna",
            CoachAssignments = [CoachAssignment(coachId, 1, hours, coachName)],
        };

    private static TrainingRequirementCoachListItem CoachAssignment(
        int coachId,
        int roleId,
        decimal hours,
        string coachName = "Jan Novák")
        => new()
        {
            Id = coachId,
            DisplayName = coachName,
            PersonalNumber = $"P{coachId}",
            CoachRoleId = roleId,
            CoachRoleName = roleId == 1 ? "Hlavní trenér" : "Asistent",
            CoachRoleOrder = roleId,
            Hours = hours,
        };

    private static TrainingPlanScheduleItemDto Plan(
        int durationMinutes,
        string phaseName = "Příprava",
        int coachId = 10)
        => new()
        {
            Id = 20,
            SeasonId = 1,
            SeasonName = "2026/2027",
            SeasonCategoryCode = "U12",
            SeasonCategoryName = "Mladší žáci",
            SeasonCategoryOrder = 2,
            TrainingTypeId = 3,
            TrainingTypeName = "Led",
            TrainingPhaseName = phaseName,
            DurationMinutes = durationMinutes,
            CoachAssignments = [PlanCoach(coachId)],
        };

    private static TrainingPlanCoachDto PlanCoach(int coachId, int roleId = 1)
        => new()
        {
            CoachId = coachId,
            FullName = coachId == 10 ? "Jan Novák" : "Petr Svoboda",
            LastName = coachId == 10 ? "Novák" : "Svoboda",
            CoachRoleId = roleId,
            CoachRoleCode = roleId == 1 ? "H" : "A",
            CoachRoleName = roleId == 1 ? "Hlavní trenér" : "Asistent",
            CoachRoleOrder = roleId,
        };
}
