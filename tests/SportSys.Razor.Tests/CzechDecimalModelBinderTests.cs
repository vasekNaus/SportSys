using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Primitives;
using SportSys.Razor.ModelBinding;
using System.Globalization;

namespace SportSys.Razor.Tests;

public class CzechDecimalModelBinderTests
{
    [Theory]
    [InlineData(typeof(decimal))]
    [InlineData(typeof(decimal?))]
    public void Provider_ReturnsBinderForDecimalTypes(Type modelType)
    {
        var provider = new CzechDecimalModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        Assert.IsType<CzechDecimalModelBinder>(provider.GetBinder(context));
    }

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(string))]
    public void Provider_ReturnsNullForOtherTypes(Type modelType)
    {
        var provider = new CzechDecimalModelBinderProvider();
        var context = new TestModelBinderProviderContext(modelType);

        Assert.Null(provider.GetBinder(context));
    }

    [Theory]
    [InlineData(typeof(decimal), "1,5", "1.5", "en-US")]
    [InlineData(typeof(decimal), "1.5", "1.5", "cs-CZ")]
    [InlineData(typeof(decimal?), "1,5", "1.5", "en-US")]
    [InlineData(typeof(decimal?), "1.5", "1.5", "cs-CZ")]
    public async Task BindModelAsync_BindsCommaAndDotSeparators(
        Type modelType,
        string value,
        string expected,
        string cultureName)
    {
        var (result, modelState) = await BindAsync(modelType, value, CultureInfo.GetCultureInfo(cultureName));

        Assert.True(result.IsModelSet);
        Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), result.Model);
        Assert.Empty(modelState.Values.SelectMany(entry => entry.Errors));
    }

    [Fact]
    public async Task BindModelAsync_EmptyNullableValueBindsNull()
    {
        var (result, modelState) = await BindAsync(typeof(decimal?), string.Empty, CultureInfo.InvariantCulture);

        Assert.True(result.IsModelSet);
        Assert.Null(result.Model);
        Assert.Empty(modelState.Values.SelectMany(entry => entry.Errors));
    }

    [Fact]
    public async Task BindModelAsync_EmptyRequiredValueAddsValidationError()
    {
        var (result, modelState) = await BindAsync(typeof(decimal), string.Empty, CultureInfo.InvariantCulture);

        Assert.False(result.IsModelSet);
        Assert.False(modelState.IsValid);
        Assert.Contains("povinné", modelState["Value"]!.Errors[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BindModelAsync_InvalidValueAddsCzechErrorAndPreservesAttemptedValue()
    {
        var (result, modelState) = await BindAsync(typeof(decimal), "1.234,5", CultureInfo.InvariantCulture);

        Assert.False(result.IsModelSet);
        Assert.False(modelState.IsValid);
        Assert.Equal("1.234,5", modelState["Value"]!.AttemptedValue);
        Assert.Contains("Zadejte číslo", modelState["Value"]!.Errors[0].ErrorMessage);
    }

    private static async Task<(ModelBindingResult Result, ModelStateDictionary ModelState)> BindAsync(
        Type modelType,
        string value,
        CultureInfo culture)
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Value"] = new StringValues(value),
        });
        var valueProvider = new QueryStringValueProvider(BindingSource.Query, query, culture);
        var metadataProvider = new EmptyModelMetadataProvider();
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var bindingContext = DefaultModelBindingContext.CreateBindingContext(
            actionContext,
            valueProvider,
            metadataProvider.GetMetadataForType(modelType),
            bindingInfo: null,
            modelName: "Value");
        var modelBinder = new CzechDecimalModelBinder();

        await modelBinder.BindModelAsync(bindingContext);

        return (bindingContext.Result, bindingContext.ModelState);
    }

    private sealed class TestModelBinderProviderContext(Type modelType) : ModelBinderProviderContext
    {
        private readonly EmptyModelMetadataProvider _metadataProvider = new();

        public override BindingInfo BindingInfo { get; } = new();

        public override ModelMetadata Metadata => _metadataProvider.GetMetadataForType(modelType);

        public override IModelMetadataProvider MetadataProvider => _metadataProvider;

        public override IModelBinder CreateBinder(ModelMetadata metadata)
            => throw new NotSupportedException();
    }
}
