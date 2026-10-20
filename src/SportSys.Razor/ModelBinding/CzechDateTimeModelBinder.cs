using Microsoft.AspNetCore.Mvc.ModelBinding;
using SportSys.Razor.Infrastructure;
using System.Globalization;

namespace SportSys.Razor.ModelBinding;

public sealed class CzechDateTimeModelBinder(Type modelType) : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult.Values.Count == 0)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var value = valueResult.FirstValue;
        if (string.IsNullOrWhiteSpace(value))
        {
            if (Nullable.GetUnderlyingType(bindingContext.ModelMetadata.ModelType) is not null)
            {
                bindingContext.Result = ModelBindingResult.Success(null);
            }
            else
            {
                bindingContext.ModelState.TryAddModelError(
                    bindingContext.ModelName,
                    "Pole je povinné.");
                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }

        if (TryParse(value, out var result))
        {
            bindingContext.Result = ModelBindingResult.Success(result);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            GetErrorMessage());
        bindingContext.Result = ModelBindingResult.Failed();
        return Task.CompletedTask;
    }

    private bool TryParse(string value, out object? result)
    {
        result = null;

        if (modelType == typeof(DateOnly) &&
            CzechDateTimeFormat.TryParseDate(value, out var date))
        {
            result = date;
            return true;
        }

        if (modelType == typeof(DateTime) &&
            CzechDateTimeFormat.TryParseDate(value, out date))
        {
            result = date.ToDateTime(TimeOnly.MinValue);
            return true;
        }

        if (modelType == typeof(TimeOnly) &&
            CzechDateTimeFormat.TryParseTime(value, out var time))
        {
            result = time;
            return true;
        }

        if (modelType == typeof(TimeSpan) &&
            CzechDateTimeFormat.TryParseTime(value, out time))
        {
            result = time.ToTimeSpan();
            return true;
        }

        return false;
    }

    private string GetErrorMessage()
        => modelType == typeof(DateOnly) || modelType == typeof(DateTime)
            ? "Zadejte datum ve formátu D. M. RRRR."
            : "Zadejte čas ve formátu HH:MM.";
}
