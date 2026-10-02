using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Razor.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace SportSys.Razor.TagHelpers;

/// <summary>
/// Znovupoužitelný víceúběr s fulltextovým vyhledáváním (Tom Select) nad
/// standardním &lt;select multiple&gt;. Generování &lt;option&gt; prvků
/// deleguje na stejnou interní metodu, kterou používá vestavěný
/// <c>SelectTagHelper</c> (<see cref="IHtmlGenerator.GenerateSelect"/>),
/// takže se zachovává standardní Razor Pages model binding (asp-for /
/// asp-items) včetně obnovení výběru po neúspěšné validaci — hodnoty se
/// čtou nejprve z ModelState (odeslaný postback) a teprve poté z modelu.
/// Klientskou inicializaci Tom Select zajišťuje auto-init v site.js podle
/// atributu <c>data-search-multiselect</c>.
/// </summary>
[HtmlTargetElement("search-multiselect", Attributes = ForAttributeName)]
public class SearchMultiSelectTagHelper : TagHelper
{
    private const string ForAttributeName = "asp-for";
    private const string ItemsAttributeName = "asp-items";

    private readonly IHtmlGenerator _htmlGenerator;

    public SearchMultiSelectTagHelper(IHtmlGenerator htmlGenerator)
    {
        _htmlGenerator = htmlGenerator;
    }

    [HtmlAttributeNotBound]
    [ViewContext]
    public ViewContext ViewContext { get; set; } = null!;

    [HtmlAttributeName(ForAttributeName)]
    public ModelExpression For { get; set; } = null!;

    [HtmlAttributeName(ItemsAttributeName)]
    public IEnumerable<SelectListItem> Items { get; set; } = [];

    [HtmlAttributeName("placeholder")]
    public string? Placeholder { get; set; }

    [HtmlAttributeName("remove-label")]
    public string RemoveLabel { get; set; } = "Odebrat";

    [HtmlAttributeName("disabled")]
    public bool Disabled { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var currentValues = _htmlGenerator.GetCurrentValues(
            ViewContext,
            For.ModelExplorer,
            For.Name,
            allowMultiple: true);

        var tagBuilder = _htmlGenerator.GenerateSelect(
            ViewContext,
            For.ModelExplorer,
            optionLabel: null,
            For.Name,
            Items,
            currentValues,
            allowMultiple: true,
            htmlAttributes: null);

        output.TagName = "select";
        output.TagMode = TagMode.StartTagAndEndTag;

        foreach (var attribute in tagBuilder.Attributes)
        {
            output.Attributes.SetAttribute(attribute.Key, attribute.Value);
        }

        output.Attributes.SetAttribute("multiple", "multiple");
        output.Attributes.SetAttribute("data-search-multiselect", "true");
        output.Attributes.SetAttribute("data-remove-label", RemoveLabel);

        if (!string.IsNullOrEmpty(Placeholder))
            output.Attributes.SetAttribute("data-placeholder", Placeholder);

        if (Disabled)
            output.Attributes.SetAttribute("disabled", "disabled");

        output.Content.SetHtmlContent(tagBuilder.InnerHtml);
    }
}
