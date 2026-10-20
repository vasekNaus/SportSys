using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SportSys.Razor.ModelBinding;

public sealed class CzechDecimalModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType)
            ?? context.Metadata.ModelType;

        return modelType == typeof(decimal)
            ? new CzechDecimalModelBinder()
            : null;
    }
}
