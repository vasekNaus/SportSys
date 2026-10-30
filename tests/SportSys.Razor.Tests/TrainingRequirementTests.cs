using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
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
using Microsoft.EntityFrameworkCore;
using SportSys.Contract.Models;
using SportSys.Contract.Services;
using SportSys.Database.Context;
using SportSys.Razor.Models;
using SportSys.Razor.Services;
using RequirementIndexModel = SportSys.Razor.Areas.sport.Pages.Training.Requirement.IndexModel;

namespace SportSys.Razor.Tests;

public class TrainingRequirementTests
{
    [Fact]
    public void CoachDisplayText_UsesDisplayNameAndRole()
    {
        var assignment = new TrainingRequirementCoachListItem
        {
            DisplayName = "Jan Novák",
            PersonalNumber = "123",
            CoachRoleName = "Hlavní trenér",
        };

        Assert.Equal("Jan Novák (Hlavní trenér)", assignment.DisplayText);
    }

    [Fact]
    public void CoachDisplayText_FallsBackToPersonalNumber()
    {
        var assignment = new TrainingRequirementCoachListItem
        {
            DisplayName = " ",
            PersonalNumber = "123",
            CoachRoleName = "Asistent",
        };

        Assert.Equal("123 (Asistent)", assignment.DisplayText);
    }

    [Fact]
    public void MatchCoachDisplayText_UsesDisplayNameAndRole()
    {
        var assignment = new MatchRequirementCoachListItem
        {
            DisplayName = "Jan Novák",
            PersonalNumber = "123",
            CoachRoleName = "Hlavní trenér",
        };

        Assert.Equal("Jan Novák (Hlavní trenér)", assignment.DisplayText);
    }

    [Fact]
    public void MatchCoachDisplayText_FallsBackToPersonalNumber()
    {
        var assignment = new MatchRequirementCoachListItem
        {
            DisplayName = " ",
            PersonalNumber = "123",
            CoachRoleName = "Asistent",
        };

        Assert.Equal("123 (Asistent)", assignment.DisplayText);
    }

    [Fact]
    public void CoachGroups_GroupByRoleAndSortLastNames()
    {
        TrainingRequirementCoachListItem Item(string name, int roleId, string role)
            => new() { DisplayName = name, CoachRoleId = roleId, CoachRoleName = role };

        var groups = SportSys.Razor.Models.RequirementCoachGroups.Group(
        [
            Item("Petr Vlasák", 6, "Kondiční"),
            Item("Václav Nauš", 1, "Hlavní"),
            Item("Jan Barčák", 1, "Hlavní"),
            Item("Karel Klaus", 2, "Asistent"),
            Item("Jan Barčák", 2, "Asistent"),
        ]);

        Assert.Equal([1, 2, 6], groups.Select(g => g.RoleId));
        Assert.Equal(["Barčák", "Nauš"], groups[0].CoachDisplayTexts);
        Assert.Equal(["Barčák", "Klaus"], groups[1].CoachDisplayTexts);
        Assert.Empty(SportSys.Razor.Models.RequirementCoachGroups.Group([]));
    }

    [Fact]
    public void CoachGroups_OrderByRoleOrderNotId()
    {
        var groups = SportSys.Razor.Models.RequirementCoachGroups.Group(
        [
            new TrainingRequirementCoachListItem { DisplayName = "A B", CoachRoleId = 1, CoachRoleOrder = 2 },
            new TrainingRequirementCoachListItem { DisplayName = "C D", CoachRoleId = 5, CoachRoleOrder = 1 },
        ]);

        Assert.Equal([5, 1], groups.Select(g => g.RoleId));
    }

    [Fact]
    public void CoachGroups_SplitsDedicatedAndOtherRolesAndFormatsTwoDecimalPlaces()
    {
        TrainingRequirementCoachListItem Item(
            string name,
            string roleCode,
            int roleId,
            int roleOrder,
            decimal hours)
            => new()
            {
                DisplayName = name,
                CoachRoleCode = roleCode,
                CoachRoleId = roleId,
                CoachRoleName = roleCode,
                CoachRoleOrder = roleOrder,
                Hours = hours,
            };

        var assignments = new[]
        {
            Item("Jan Novák", "H", 1, 1, 2m),
            Item("Petr Zíma", "A", 2, 2, 1.5m),
            Item("Karel Černý", "B", 4, 4, 0.5m),
            Item("Václav Dvořák", "O", 3, 3, 1m),
            Item("Pavel Kříž", "K", 6, 6, 0m),
            Item("Petr Fišer", "F", 5, 5, 999.99m),
        };

        Assert.Equal(["Novák (2)"], RequirementCoachGroups.ForRole(assignments, "H"));
        Assert.Equal(["Zíma (1,5)"], RequirementCoachGroups.ForRole(assignments, "A"));
        Assert.Equal(["Černý (0,5)"], RequirementCoachGroups.ForRole(assignments, "B"));
        Assert.Equal(["Dvořák (1)"], RequirementCoachGroups.ForRole(assignments, "O"));
        Assert.Empty(RequirementCoachGroups.ForRole(assignments, "X"));

        var others = RequirementCoachGroups.OtherRoles(assignments);
        Assert.Equal(["K", "F"], others.Select(assignment => assignment.CoachRoleCode));
        Assert.Equal(
            ["F (Fišer (999,99))", "K (Kříž (0))"],
            RequirementCoachGroups.Group(others, includeHours: true)
                .Select(group => $"{group.RoleName} ({string.Join(", ", group.CoachDisplayTexts)})"));
    }

    [Theory]
    [InlineData("0", "0")]
    [InlineData("0.5", "0,5")]
    [InlineData("1", "1")]
    [InlineData("1.25", "1,25")]
    [InlineData("1.5", "1,5")]
    [InlineData("2", "2")]
    [InlineData("999.99", "999,99")]
    public void CoachGroups_WithHours_UsesOptionalCzechDecimalsRegardlessOfCurrentCulture(
        string hours,
        string expected)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            var groups = RequirementCoachGroups.Group(
            [
                new TrainingRequirementCoachListItem
                {
                    DisplayName = "Jan Novák",
                    Hours = decimal.Parse(hours, CultureInfo.InvariantCulture),
                },
            ], includeHours: true);

            Assert.Equal($"Novák ({expected})", Assert.Single(Assert.Single(groups).CoachDisplayTexts));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void CoachGroups_WithHours_SortsRolesAndCzechLastNamesBeforeFormatting()
    {
        TrainingRequirementCoachListItem Item(string name, int roleId, int roleOrder, decimal hours)
            => new()
            {
                DisplayName = name,
                CoachRoleId = roleId,
                CoachRoleOrder = roleOrder,
                Hours = hours,
            };

        var groups = RequirementCoachGroups.Group(
        [
            Item("Jan Novák", 1, 2, 0.5m),
            Item("Jan Chládek", 5, 1, 0m),
            Item("Petr Černý", 5, 1, 1.5m),
            Item("Jan Havlíček", 5, 1, 2m),
        ], includeHours: true);

        Assert.Equal([5, 1], groups.Select(group => group.RoleId));
        Assert.Equal(
            ["Černý (1,5)", "Havlíček (2)", "Chládek (0)"],
            groups[0].CoachDisplayTexts);
        Assert.Equal(["Novák (0,5)"], groups[1].CoachDisplayTexts);
    }

    [Theory]
    [InlineData("1.5", "1,5")]
    [InlineData("2", "2")]
    public void CoachGroups_WithHours_PreservesDifferentCoachesWithSameLastName(
        string secondHours,
        string expectedSecondHours)
    {
        var groups = RequirementCoachGroups.Group(
        [
            new TrainingRequirementCoachListItem { Id = 1, DisplayName = "Jan Novák", Hours = 2m },
            new TrainingRequirementCoachListItem
            {
                Id = 2,
                DisplayName = "Petr Novák",
                Hours = decimal.Parse(secondHours, CultureInfo.InvariantCulture),
            },
        ], includeHours: true);

        Assert.Equal(
            ["Novák (2)", $"Novák ({expectedSecondHours})"],
            Assert.Single(groups).CoachDisplayTexts);
    }

    [Fact]
    public void CoachGroups_WithHours_PreservesHoursForSameCoachInDifferentRoles()
    {
        var groups = RequirementCoachGroups.Group(
        [
            new TrainingRequirementCoachListItem
            {
                Id = 1, DisplayName = "Jan Novák", CoachRoleId = 1,
                CoachRoleName = "Hlavní", CoachRoleOrder = 1, Hours = 2m,
            },
            new TrainingRequirementCoachListItem
            {
                Id = 1, DisplayName = "Jan Novák", CoachRoleId = 2,
                CoachRoleName = "Asistent", CoachRoleOrder = 2, Hours = 1.5m,
            },
        ], includeHours: true);

        Assert.Equal(["Hlavní", "Asistent"], groups.Select(group => group.RoleName));
        Assert.Equal(["Novák (2)"], groups[0].CoachDisplayTexts);
        Assert.Equal(["Novák (1,5)"], groups[1].CoachDisplayTexts);
    }

    [Fact]
    public void CoachGroups_WithHours_UsesPersonalNumberAndHandlesEmptyAssignments()
    {
        var groups = RequirementCoachGroups.Group(
        [
            new TrainingRequirementCoachListItem { DisplayName = " ", PersonalNumber = "123", Hours = 0.5m },
        ], includeHours: true);

        Assert.Equal(["123 (0,5)"], Assert.Single(groups).CoachDisplayTexts);
        Assert.Empty(RequirementCoachGroups.Group([], includeHours: true));
    }

    [Fact]
    public void CoachGroups_WithoutHours_PreservesMatchLastNameDeduplication()
    {
        var groups = RequirementCoachGroups.Group(
        [
            new MatchRequirementCoachListItem { Id = 1, DisplayName = "Jan Novák", Hours = 2m },
            new MatchRequirementCoachListItem { Id = 2, DisplayName = "Petr Novák", Hours = 1.5m },
            new MatchRequirementCoachListItem { Id = 3, DisplayName = "Karel Klaus" },
        ]);

        Assert.Equal(["Klaus", "Novák"], Assert.Single(groups).CoachDisplayTexts);
    }

    [Fact]
    public async Task RequirementViews_KeepTrainingFormattingAndAlignmentSeparateFromMatches()
    {
        IReadOnlyList<RequirementCoachListItem> assignments =
        [
            new TrainingRequirementCoachListItem
            {
                DisplayName = "Jan Novák", CoachRoleName = "Hlavní", CoachRoleCode = "H",
                CoachRoleId = 1, CoachRoleOrder = 1, Hours = 1.5m,
            },
        ];
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ApplicationName = typeof(RequirementIndexModel).Assembly.GetName().Name,
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
            "Requirement test"));
        var viewResult = services.GetRequiredService<IRazorViewEngine>().GetView(
            null,
            "/Areas/sport/Pages/Training/Requirement/_CoachGroups.cshtml",
            isMainPage: false);
        Assert.True(viewResult.Success);

        var viewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        {
            Model = assignments,
        };
        var trainingViewData = new ViewDataDictionary(viewData) { ["IncludeHours"] = true };
        var tempData = new TempDataDictionary(actionContext.HttpContext, services.GetRequiredService<ITempDataProvider>());

        async Task<string> RenderAsync(
            ViewDataDictionary data,
            Microsoft.AspNetCore.Mvc.ViewEngines.IView? view = null)
        {
            using var writer = new StringWriter();
            var context = new ViewContext(
                actionContext, view ?? viewResult.View, data, tempData, writer, new HtmlHelperOptions());
            await context.View.RenderAsync(context);
            return writer.ToString();
        }

        var trainingHtml = WebUtility.HtmlDecode(await RenderAsync(trainingViewData));
        var matchHtml = WebUtility.HtmlDecode(await RenderAsync(viewData));
        Assert.Contains("Hlavní:", trainingHtml);
        Assert.Contains("Novák (1,5)", trainingHtml);
        Assert.Contains("Novák", matchHtml);
        Assert.DoesNotContain("(1,5)", matchHtml);
        Assert.Null(viewData["IncludeHours"]);

        viewData.Model = Array.Empty<RequirementCoachListItem>();
        Assert.Equal("-", (await RenderAsync(viewData)).Trim());

        viewData.Model = new[]
        {
            new TrainingRequirementCoachListItem { DisplayName = "Jan <script>", CoachRoleName = "<b>" },
        };
        var encodedHtml = await RenderAsync(viewData);
        Assert.Contains("&lt;script&gt;", encodedHtml);
        Assert.Contains("&lt;b&gt;", encodedHtml);
        Assert.DoesNotContain("<script>", encodedHtml);

        actionContext.RouteData.Values["area"] = "sport";
        actionContext.RouteData.Values["page"] = "/Training/Requirement/Index";
        actionContext.ActionDescriptor = new Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor
        {
            ViewEnginePath = "/Training/Requirement/Index",
            RelativePath = "/Areas/sport/Pages/Training/Requirement/Index.cshtml",
            RouteValues = new Dictionary<string, string?>
            {
                ["area"] = "sport",
                ["page"] = "/Training/Requirement/Index",
            },
        };
        using var db = new SportSysDbContext(
            new DbContextOptionsBuilder<SportSysDbContext>().Options);
        var indexModel = new RequirementIndexModel(
            new TrainingRequirementService(db),
            new MatchRequirementService(db),
            new TrainingScheduleService(db),
            new TrainingOverviewExcelExporter());
        indexModel.Seasons.Add(new SeasonDto { Id = 1, Name = "2026/2027" });
        indexModel.Requirements.Add(new TrainingRequirementListItem
        {
            Id = 42,
            SeasonName = "2026/2027",
            SeasonCategoryName = "Mladší žáci",
            TrainingTypeName = "Led",
            TrainingPhaseName = "Sezóna",
            From = new DateOnly(2026, 9, 1),
            To = new DateOnly(2027, 3, 31),
            DurationHours = 2.5m,
            CoachAssignments =
            [
                new()
                {
                    Id = 1, DisplayName = "Jan Hlavní", CoachRoleCode = "H",
                    CoachRoleId = 1, CoachRoleName = "Hlavní trenér", CoachRoleOrder = 1, Hours = 1.5m,
                },
                new()
                {
                    Id = 2, DisplayName = "Jan Asistent", CoachRoleCode = "A",
                    CoachRoleId = 2, CoachRoleName = "Asistent trenéra", CoachRoleOrder = 2, Hours = 2m,
                },
                new()
                {
                    Id = 3, DisplayName = "Jan Brankář", CoachRoleCode = "B",
                    CoachRoleId = 4, CoachRoleName = "Brankářský trenér", CoachRoleOrder = 4, Hours = 0.5m,
                },
                new()
                {
                    Id = 4, DisplayName = "Jan Obránce", CoachRoleCode = "O",
                    CoachRoleId = 3, CoachRoleName = "Trenér obránců", CoachRoleOrder = 3, Hours = 1m,
                },
                new()
                {
                    Id = 5, DisplayName = "Jan Fyzioterapeut", CoachRoleCode = "F",
                    CoachRoleId = 5, CoachRoleName = "Fyzioterapeut", CoachRoleOrder = 5, Hours = 0.75m,
                },
            ],
        });
        indexModel.MatchRequirements.Add(new MatchRequirementListItem
        {
            SeasonCategoryCode = "U12",
        });
        var indexView = services.GetRequiredService<IRazorViewEngine>().GetView(
            null,
            "/Areas/sport/Pages/Training/Requirement/Index.cshtml",
            isMainPage: false);
        Assert.True(indexView.Success);
        var indexViewData = new ViewDataDictionary<RequirementIndexModel>(
            new EmptyModelMetadataProvider(),
            new ModelStateDictionary())
        {
            Model = indexModel,
        };
        var page = Assert.IsAssignableFrom<Microsoft.AspNetCore.Mvc.RazorPages.Page>(
            Assert.IsType<RazorView>(indexView.View).RazorPage);
        page.PageContext = new Microsoft.AspNetCore.Mvc.RazorPages.PageContext(actionContext)
        {
            ViewData = indexViewData,
        };
        indexModel.PageContext = page.PageContext;
        var indexHtml = await RenderAsync(indexViewData, indexView.View);
        var tables = Regex.Matches(indexHtml, @"<table\b[^>]*>.*?</table>", RegexOptions.Singleline);
        Assert.Equal(2, tables.Count);
        var trainingHeaders = Regex.Matches(tables[0].Value, @"<th\b[^>]*>(.*?)</th>", RegexOptions.Singleline)
            .Select(header => WebUtility.HtmlDecode(Regex.Replace(header.Groups[1].Value, "<.*?>", string.Empty)).Trim());
        Assert.Equal(
            ["Sezóna", "Kategorie", "Typ tréninku", "Fáze", "Od", "Do", "Rozsah [h]",
             "Hlavní", "Asistent", "Brankáři", "Obránci", "Trenéři", "Akce"],
            trainingHeaders);
        Assert.Equal(13, Regex.Matches(tables[0].Value, @"<th\b[^>]*>").Count);
        Assert.All(
            Regex.Matches(tables[0].Value, @"<th\b[^>]*>").Cast<Match>(),
            header => Assert.Equal("<th class=\"c\">", header.Value));
        var trainingCells = Regex.Matches(
                tables[0].Value,
                @"<td\b[^>]*>(.*?)</td>",
                RegexOptions.Singleline)
            .Select(cell => Regex.Replace(
                WebUtility.HtmlDecode(Regex.Replace(cell.Groups[1].Value, "<.*?>", string.Empty)),
                @"\s+",
                " ").Trim())
            .ToList();
        Assert.Equal(
            ["<td class=\"c\">", "<td>", "<td>", "<td class=\"c\">",
             "<td class=\"c\">", "<td class=\"c\">", "<td class=\"r\">",
             "<td>", "<td>", "<td>", "<td>", "<td>", "<td class=\"buttons c\">"],
            Regex.Matches(tables[0].Value, @"<td\b[^>]*>").Select(cell => cell.Value));
        Assert.Equal(13, trainingCells.Count);
        Assert.Equal("Hlavní (1,5)", trainingCells[7]);
        Assert.Equal("Asistent (2)", trainingCells[8]);
        Assert.Equal("Brankář (0,5)", trainingCells[9]);
        Assert.Equal("Obránce (1)", trainingCells[10]);
        Assert.Equal("Fyzioterapeut: Fyzioterapeut (0,75)", trainingCells[11]);
        Assert.Contains("<a class=\"button secondary icon-btn\"", tables[0].Value);
        Assert.Contains("title=\"Upravit\"", tables[0].Value);
        Assert.All(
            Regex.Matches(tables[1].Value, @"<th\b[^>]*>").Cast<Match>(),
            header => Assert.Equal("<th>", header.Value));
        Assert.Equal(
            ["<td>", "<td>", "<td>", "<td>", "<td>", "<td>", "<td class=\"buttons\">"],
            Regex.Matches(tables[1].Value, @"<td\b[^>]*>").Select(cell => cell.Value));
    }

    [Fact]
    public void CoachRoleSeed_HasUniqueCodeAndOrder()
    {
        var roles = Enum.GetValues<SportSys.Database.Enums.ECoachRole>()
            .Select(e => new SportSys.Database.Models.dboSchema.CoachRole(e))
            .ToList();

        Assert.All(roles, role => Assert.Equal(1, role.Code.Length));
        Assert.Equal(roles.Count, roles.Select(role => role.Code).Distinct().Count());
        Assert.Equal(roles.Count, roles.Select(role => role.Order).Distinct().Count());
    }

    [Fact]
    public void CoachLastName_UsesLastWordOrPersonalNumber()
    {
        Assert.Equal("Nauš", new TrainingRequirementCoachListItem { DisplayName = "Václav Nauš" }.LastName);
        Assert.Equal("123", new TrainingRequirementCoachListItem { DisplayName = " ", PersonalNumber = "123" }.LastName);
    }

    [Fact]
    public void RequirementCoachItems_ShareCommonBase()
    {
        Assert.IsAssignableFrom<RequirementCoachListItem>(new TrainingRequirementCoachListItem());
        Assert.IsAssignableFrom<RequirementCoachListItem>(new MatchRequirementCoachListItem());
    }

    [Fact]
    public void NormalizeIds_RemovesInvalidAndDuplicateValues()
    {
        var availableItems = new[]
        {
            new LookupSelectItem { Id = 2, Name = "Druhý" },
            new LookupSelectItem { Id = 1, Name = "První" },
        };

        var result = RequirementIndexModel.NormalizeIds(
            [1, 3, 1, 2],
            availableItems);

        Assert.Equal([2, 1], result);
    }
}
