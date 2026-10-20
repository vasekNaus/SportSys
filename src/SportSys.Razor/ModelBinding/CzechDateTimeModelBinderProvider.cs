using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations;

namespace SportSys.Razor.ModelBinding;

public sealed class CzechDateTimeModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType)
            ?? context.Metadata.ModelType;

        var isDate = modelType == typeof(DateOnly) ||
                     modelType == typeof(DateTime) &&
                     context.Metadata.DataTypeName == DataType.Date.ToString();
        var isTime = modelType == typeof(TimeOnly) ||
                     modelType == typeof(TimeSpan) &&
                     context.Metadata.DataTypeName == DataType.Time.ToString();

        return isDate || isTime
            ? new CzechDateTimeModelBinder(modelType)
            : null;
    }
}
