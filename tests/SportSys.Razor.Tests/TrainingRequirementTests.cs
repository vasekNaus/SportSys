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

    [Theory]
    [InlineData("0", "0,00")]
    [InlineData("0.5", "0,50")]
    [InlineData("1", "1,00")]
    [InlineData("1.25", "1,25")]
    [InlineData("1.5", "1,50")]
    [InlineData("2", "2,00")]
    [InlineData("999.99", "999,99")]
    public void CoachGroups_WithHours_UsesTwoCzechDecimalPlacesRegardlessOfCurrentCulture(
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
            ["Černý (1,50)", "Havlíček (2,00)", "Chládek (0,00)"],
            groups[0].CoachDisplayTexts);
        Assert.Equal(["Novák (0,50)"], groups[1].CoachDisplayTexts);
    }

    [Theory]
    [InlineData("1.5", "1,50")]
    [InlineData("2", "2,00")]
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
            ["Novák (2,00)", $"Novák ({expectedSecondHours})"],
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
        Assert.Equal(["Novák (2,00)"], groups[0].CoachDisplayTexts);
        Assert.Equal(["Novák (1,50)"], groups[1].CoachDisplayTexts);
    }

    [Fact]
    public void CoachGroups_WithHours_UsesPersonalNumberAndHandlesEmptyAssignments()
    {
        var groups = RequirementCoachGroups.Group(
        [
            new TrainingRequirementCoachListItem { DisplayName = " ", PersonalNumber = "123", Hours = 0.5m },
        ], includeHours: true);

        Assert.Equal(["123 (0,50)"], Assert.Single(groups).CoachDisplayTexts);
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
    public async Task CoachGroupsPartial_UsesLocalHoursFlagWithoutChangingMatchRendering()
    {
        IReadOnlyList<RequirementCoachListItem> assignments =
        [
            new TrainingRequirementCoachListItem
            {
                DisplayName = "Jan Novák", CoachRoleName = "Hlavní", Hours = 1.5m,
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

        async Task<string> RenderAsync(ViewDataDictionary data)
        {
            using var writer = new StringWriter();
            var context = new ViewContext(
                actionContext, viewResult.View, data, tempData, writer, new HtmlHelperOptions());
            await viewResult.View.RenderAsync(context);
            return writer.ToString();
        }

        var trainingHtml = WebUtility.HtmlDecode(await RenderAsync(trainingViewData));
        var matchHtml = WebUtility.HtmlDecode(await RenderAsync(viewData));
        Assert.Contains("Hlavní:", trainingHtml);
        Assert.Contains("Novák (1,50)", trainingHtml);
        Assert.Contains("Novák", matchHtml);
        Assert.DoesNotContain("(1,50)", matchHtml);
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
