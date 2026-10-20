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

public class CzechDateTimeModelBinderTests
{
    [Theory]
    [InlineData("5. 10. 2026")]
    [InlineData("5.10.2026")]
    [InlineData("05.10.2026")]
    [InlineData("2026-10-05")]
    public async Task BindModelAsync_BindsCzechAndIsoDatesFromInvariantQueryProvider(string value)
    {
        var (result, modelState) = await BindAsync(typeof(DateOnly), value);

        Assert.Equal(value, modelState["Value"]!.AttemptedValue);
        Assert.Empty(modelState["Value"]!.Errors);
        Assert.True(result.IsModelSet);
        Assert.Equal(new DateOnly(2026, 10, 5), result.Model);
    }

    [Fact]
    public async Task BindModelAsync_BindsNullableDateFromInvariantQueryProvider()
    {
        var (result, modelState) = await BindAsync(typeof(DateOnly?), "5. 10. 2026");

        Assert.True(result.IsModelSet);
        Assert.Equal(new DateOnly(2026, 10, 5), result.Model);
        Assert.Empty(modelState.Values.SelectMany(entry => entry.Errors));
    }

    [Theory]
    [InlineData("7:05", "07:05")]
    [InlineData("07:05", "07:05")]
    [InlineData("19:30", "19:30")]
    public async Task BindModelAsync_BindsTwentyFourHourTimes(string value, string expected)
    {
        var (result, modelState) = await BindAsync(typeof(TimeOnly), value);

        Assert.True(result.IsModelSet);
        Assert.Equal(TimeOnly.ParseExact(expected, "HH:mm", CultureInfo.InvariantCulture), result.Model);
        Assert.Empty(modelState.Values.SelectMany(entry => entry.Errors));
    }

    [Fact]
    public async Task BindModelAsync_BindsDateTimeAsDateAtMidnight()
    {
        var (result, modelState) = await BindAsync(typeof(DateTime), "5. 10. 2026");

        Assert.True(result.IsModelSet);
        Assert.Equal(new DateTime(2026, 10, 5), result.Model);
        Assert.Empty(modelState.Values.SelectMany(entry => entry.Errors));
    }

    [Fact]
    public async Task BindModelAsync_BindsTimeSpanFromTwentyFourHourTime()
    {
        var (result, modelState) = await BindAsync(typeof(TimeSpan), "07:05");

        Assert.True(result.IsModelSet);
        Assert.Equal(new TimeSpan(7, 5, 0), result.Model);
        Assert.Empty(modelState.Values.SelectMany(entry => entry.Errors));
    }

    [Fact]
    public async Task BindModelAsync_EmptyNullableValueBindsNull()
    {
        var (result, modelState) = await BindAsync(typeof(DateOnly?), string.Empty);

        Assert.True(result.IsModelSet);
        Assert.Null(result.Model);
        Assert.Empty(modelState.Values.SelectMany(entry => entry.Errors));
    }

    [Fact]
    public async Task BindModelAsync_EmptyRequiredValueAddsValidationError()
    {
        var (result, modelState) = await BindAsync(typeof(DateOnly), string.Empty);

        Assert.False(result.IsModelSet);
        Assert.False(modelState.IsValid);
        Assert.Contains("povinné", modelState["Value"]!.Errors[0].ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(DateOnly), "10/05/2026", "datum")]
    [InlineData(typeof(DateOnly), "31. 2. 2026", "datum")]
    [InlineData(typeof(TimeOnly), "25:00", "čas")]
    [InlineData(typeof(TimeOnly), "abc", "čas")]
    public async Task BindModelAsync_InvalidValueAddsCzechModelError(Type modelType, string value, string expectedMessage)
    {
        var (result, modelState) = await BindAsync(modelType, value);

        Assert.False(result.IsModelSet);
        Assert.False(modelState.IsValid);
        Assert.Contains(
            expectedMessage,
            modelState["Value"]!.Errors[0].ErrorMessage,
            StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<(ModelBindingResult Result, ModelStateDictionary ModelState)> BindAsync(
        Type modelType,
        string value)
    {
        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            ["Value"] = new StringValues(value),
        });
        var valueProvider = new QueryStringValueProvider(
            BindingSource.Query,
            query,
            CultureInfo.InvariantCulture);
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
        Assert.Equal("Value", bindingContext.ModelName);
        Assert.NotEqual(ValueProviderResult.None, bindingContext.ValueProvider.GetValue(bindingContext.ModelName));
        var bindingValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        Assert.Equal(value, bindingValue.FirstValue);
        Assert.NotEqual(0, bindingValue.Values.Count);
        var modelBinder = new CzechDateTimeModelBinder(
            Nullable.GetUnderlyingType(modelType) ?? modelType);

        await modelBinder.BindModelAsync(bindingContext);

        return (bindingContext.Result, bindingContext.ModelState);
    }
}
