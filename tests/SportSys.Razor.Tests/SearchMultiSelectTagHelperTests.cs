using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using SportSys.Razor.TagHelpers;

namespace SportSys.Razor.Tests;

/// <summary>
/// Testuje vlastní logiku <see cref="SearchMultiSelectTagHelper"/> (slučování
/// atributů, placeholder, disabled, předání aktuálních hodnot do generátoru).
/// Samotné generování &lt;option&gt; prvků a vyhodnocení ModelState vs. model
/// hodnoty je důvěryhodná funkcionalita ASP.NET Core (<see cref="IHtmlGenerator"/>),
/// proto je zde nahrazena jednoduchým testovacím dvojníkem
/// (<see cref="FakeHtmlGenerator"/>), který věrně simuluje jeho chování.
/// </summary>
public class SearchMultiSelectTagHelperTests
{
    [Fact]
    public void Process_PassesCurrentValuesFromModelToGenerator()
    {
        var model = new TestModel { CoachIds = [2] };
        var (tagHelper, generator) = CreateTagHelper(model);

        var output = Process(tagHelper);

        Assert.Equal(["2"], generator.LastCurrentValues);
        var html = output.Content.GetContent();
        Assert.Contains("value=\"2\" selected=\"selected\"", html);
        Assert.DoesNotContain("value=\"1\" selected=\"selected\"", html);
    }

    [Fact]
    public void Process_PrefersModelStateValuesAfterFailedValidation()
    {
        // Model stále obsahuje původní výběr (2), ale uživatel odeslal jiný
        // (1) — po neúspěšné validaci musí zůstat zachovaný odeslaný výběr,
        // ne výběr z modelu.
        var model = new TestModel { CoachIds = [2] };
        var (tagHelper, generator) = CreateTagHelper(model);
        tagHelper.ViewContext.ViewData.ModelState.SetModelValue(
            "CoachIds",
            new ValueProviderResult(new StringValues("1")));

        var output = Process(tagHelper);

        Assert.Equal(["1"], generator.LastCurrentValues);
        var html = output.Content.GetContent();
        Assert.Contains("value=\"1\" selected=\"selected\"", html);
        Assert.DoesNotContain("value=\"2\" selected=\"selected\"", html);
    }

    [Fact]
    public void Process_SetsExpectedAttributesForClientInit()
    {
        var model = new TestModel();
        var (tagHelper, _) = CreateTagHelper(model);
        tagHelper.Placeholder = "Vyber trenéry...";
        tagHelper.RemoveLabel = "Smazat";

        var output = Process(tagHelper);

        Assert.Equal("select", output.TagName);
        Assert.Equal("multiple", output.Attributes["multiple"].Value);
        Assert.Equal("true", output.Attributes["data-search-multiselect"].Value);
        Assert.Equal("Vyber trenéry...", output.Attributes["data-placeholder"].Value);
        Assert.Equal("Smazat", output.Attributes["data-remove-label"].Value);
        Assert.Equal("CoachIds", output.Attributes["name"].Value);
    }

    [Fact]
    public void Process_WithoutPlaceholder_DoesNotEmitPlaceholderAttribute()
    {
        var model = new TestModel();
        var (tagHelper, _) = CreateTagHelper(model);

        var output = Process(tagHelper);

        Assert.False(output.Attributes.ContainsName("data-placeholder"));
    }

    [Fact]
    public void Process_DisabledAddsDisabledAttribute()
    {
        var model = new TestModel();
        var (tagHelper, _) = CreateTagHelper(model);
        tagHelper.Disabled = true;

        var output = Process(tagHelper);

        Assert.Equal("disabled", output.Attributes["disabled"].Value);
    }

    [Fact]
    public void Process_PreservesAttributesAuthoredOnTheElement()
    {
        // Atributy jako aria-labelledby, které TagHelper sám nenastavuje,
        // musí projít z původního elementu beze změny (viz použití v
        // Training/Plan/Edit.cshtml).
        var model = new TestModel();
        var (tagHelper, _) = CreateTagHelper(model);

        var context = new TagHelperContext(
            "search-multiselect",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());

        var output = new TagHelperOutput(
            "search-multiselect",
            new TagHelperAttributeList { { "aria-labelledby", "CoachesLabel" } },
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        tagHelper.Process(context, output);

        Assert.Equal("CoachesLabel", output.Attributes["aria-labelledby"].Value);
    }

    private static TagHelperOutput Process(SearchMultiSelectTagHelper tagHelper)
    {
        var context = new TagHelperContext(
            "search-multiselect",
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            Guid.NewGuid().ToString());

        var output = new TagHelperOutput(
            "search-multiselect",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) => Task.FromResult<TagHelperContent>(new DefaultTagHelperContent()));

        tagHelper.Process(context, output);
        return output;
    }

    private static (SearchMultiSelectTagHelper TagHelper, FakeHtmlGenerator Generator) CreateTagHelper(TestModel model)
    {
        var metadataProvider = new DefaultModelMetadataProvider(new NoOpMetadataDetailsProvider());

        var viewData = new ViewDataDictionary<TestModel>(metadataProvider, new ModelStateDictionary())
        {
            Model = model,
        };

        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());

        var viewContext = new ViewContext(
            actionContext,
            new FakeView(),
            viewData,
            new FakeTempData(),
            TextWriter.Null,
            new HtmlHelperOptions());

        var generator = new FakeHtmlGenerator();

        var containerExplorer = metadataProvider.GetModelExplorerForType(typeof(TestModel), model);
        var coachIdsExplorer = containerExplorer.GetExplorerForProperty(nameof(TestModel.CoachIds))!;
        var modelExpression = new ModelExpression(nameof(TestModel.CoachIds), coachIdsExplorer);

        var tagHelper = new SearchMultiSelectTagHelper(generator)
        {
            ViewContext = viewContext,
            For = modelExpression,
            Items =
            [
                new SelectListItem("Jan Novák", "1"),
                new SelectListItem("Petr Svoboda", "2"),
            ],
        };

        return (tagHelper, generator);
    }

    private sealed class TestModel
    {
        public List<int> CoachIds { get; set; } = [];
    }

    private sealed class NoOpMetadataDetailsProvider : ICompositeMetadataDetailsProvider
    {
        public void CreateBindingMetadata(BindingMetadataProviderContext context) { }

        public void CreateDisplayMetadata(DisplayMetadataProviderContext context) { }

        public void CreateValidationMetadata(ValidationMetadataProviderContext context) { }
    }

    private sealed class FakeView : Microsoft.AspNetCore.Mvc.ViewEngines.IView
    {
        public string Path => string.Empty;

        public Task RenderAsync(ViewContext context) => throw new NotSupportedException();
    }

    private sealed class FakeTempData : Dictionary<string, object?>, ITempDataDictionary
    {
        public void Keep() { }

        public void Keep(string key) { }

        public void Load() { }

        public void Save() { }

        public object? Peek(string key) => TryGetValue(key, out var value) ? value : null;
    }

    /// <summary>
    /// Testovací dvojník <see cref="IHtmlGenerator"/>. Implementuje jen
    /// <see cref="GetCurrentValues"/> a osmiparametrovou variantu
    /// <see cref="GenerateSelect"/>, které <see cref="SearchMultiSelectTagHelper"/>
    /// skutečně používá — věrně napodobuje prioritu ModelState → model, kterou
    /// má i skutečný <c>DefaultHtmlGenerator</c>. Ostatní členy rozhraní nejsou
    /// touto komponentou volány, proto vyhazují <see cref="NotSupportedException"/>.
    /// </summary>
    private sealed class FakeHtmlGenerator : IHtmlGenerator
    {
        public ICollection<string>? LastCurrentValues { get; private set; }

        public string IdAttributeDotReplacement => "_";

        public ICollection<string> GetCurrentValues(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string expression,
            bool allowMultiple)
        {
            if (viewContext.ViewData.ModelState.TryGetValue(expression, out var entry) &&
                entry.RawValue is not null)
            {
                return entry.RawValue switch
                {
                    StringValues sv => sv.ToArray()!,
                    string[] arr => arr!,
                    string s => [s],
                    _ => [],
                };
            }

            if (modelExplorer.Model is System.Collections.IEnumerable enumerable and not string)
            {
                return enumerable
                    .Cast<object?>()
                    .Select(value => value?.ToString() ?? string.Empty)
                    .ToList();
            }

            return modelExplorer.Model is null
                ? []
                : [modelExplorer.Model.ToString() ?? string.Empty];
        }

        public TagBuilder GenerateSelect(
            ViewContext viewContext,
            ModelExplorer modelExplorer,
            string? optionLabel,
            string expression,
            IEnumerable<SelectListItem> selectList,
            ICollection<string> currentValues,
            bool allowMultiple,
            object? htmlAttributes)
        {
            LastCurrentValues = currentValues;

            var tagBuilder = new TagBuilder("select");
            tagBuilder.Attributes["name"] = expression;
            tagBuilder.Attributes["id"] = expression.Replace('.', '_');

            var content = string.Concat(selectList.Select(item =>
            {
                var selected = currentValues.Contains(item.Value ?? string.Empty)
                    ? " selected=\"selected\""
                    : string.Empty;
                return $"<option value=\"{item.Value}\"{selected}>{item.Text}</option>";
            }));

            tagBuilder.InnerHtml.AppendHtml(content);
            return tagBuilder;
        }

        public string Encode(string value) => throw new NotSupportedException();

        public string Encode(object value) => throw new NotSupportedException();

        public string FormatValue(object? value, string? format) => throw new NotSupportedException();

        public TagBuilder GenerateActionLink(ViewContext viewContext, string linkText, string actionName, string controllerName, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GeneratePageLink(ViewContext viewContext, string linkText, string pageName, string pageHandler, string protocol, string hostname, string fragment, object routeValues, object htmlAttributes) => throw new NotSupportedException();

        public Microsoft.AspNetCore.Html.IHtmlContent GenerateAntiforgery(ViewContext viewContext) => throw new NotSupportedException();

        public TagBuilder GenerateCheckBox(ViewContext viewContext, ModelExplorer modelExplorer, string expression, bool? isChecked, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateHiddenForCheckbox(ViewContext viewContext, ModelExplorer modelExplorer, string expression) => throw new NotSupportedException();

        public TagBuilder GenerateForm(ViewContext viewContext, string actionName, string controllerName, object routeValues, string method, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GeneratePageForm(ViewContext viewContext, string pageName, string pageHandler, object routeValues, string fragment, string method, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateRouteForm(ViewContext viewContext, string routeName, object routeValues, string method, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateHidden(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, bool useViewData, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateLabel(ViewContext viewContext, ModelExplorer modelExplorer, string expression, string? labelText, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GeneratePassword(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateRadioButton(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, bool? isChecked, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateRouteLink(ViewContext viewContext, string linkText, string routeName, string protocol, string hostName, string fragment, object routeValues, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateSelect(ViewContext viewContext, ModelExplorer modelExplorer, string? optionLabel, string expression, IEnumerable<SelectListItem> selectList, bool allowMultiple, object htmlAttributes) => throw new NotSupportedException();

        public Microsoft.AspNetCore.Html.IHtmlContent GenerateGroupsAndOptions(string? optionLabel, IEnumerable<SelectListItem> selectList) => throw new NotSupportedException();

        public TagBuilder GenerateTextArea(ViewContext viewContext, ModelExplorer modelExplorer, string expression, int rows, int columns, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateTextBox(ViewContext viewContext, ModelExplorer modelExplorer, string expression, object value, string format, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateValidationMessage(ViewContext viewContext, ModelExplorer modelExplorer, string expression, string? message, string? tag, object htmlAttributes) => throw new NotSupportedException();

        public TagBuilder GenerateValidationSummary(ViewContext viewContext, bool excludePropertyErrors, string? message, string? headerTag, object htmlAttributes) => throw new NotSupportedException();
    }
}
