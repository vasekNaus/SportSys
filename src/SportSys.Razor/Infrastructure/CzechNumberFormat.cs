using System.Globalization;

namespace SportSys.Razor.Infrastructure;

public static class CzechNumberFormat
{
    private static readonly CultureInfo CzechCulture = CultureInfo.GetCultureInfo("cs-CZ");

    public static string FormatDecimal(object? value, string? format = null)
    {
        if (value is not IFormattable formattable ||
            value is not decimal and not double and not float)
        {
            return string.Empty;
        }

        if (string.IsNullOrEmpty(format))
        {
            return formattable.ToString(null, CzechCulture);
        }

        return format.Contains("{0", StringComparison.Ordinal)
            ? string.Format(CzechCulture, format, value)
            : formattable.ToString(format, CzechCulture);
    }

    public static bool TryParseDecimal(string? value, out decimal result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("\u00A0", string.Empty, StringComparison.Ordinal)
            .Replace("\u202F", string.Empty, StringComparison.Ordinal);

        var commaCount = normalized.Count(character => character == ',');
        var dotCount = normalized.Count(character => character == '.');
        if (commaCount > 1 || dotCount > 1 || commaCount > 0 && dotCount > 0)
        {
            return false;
        }

        normalized = normalized.Replace(',', '.');
        var digitCount = 0;
        var decimalSeparatorIndex = normalized.IndexOf('.');
        for (var index = 0; index < normalized.Length; index++)
        {
            if (index == 0 && normalized[index] is '+' or '-')
            {
                continue;
            }

            if (char.IsAsciiDigit(normalized[index]))
            {
                digitCount++;
                continue;
            }

            if (normalized[index] != '.' || index != decimalSeparatorIndex)
            {
                return false;
            }
        }

        if (digitCount == 0 ||
            decimalSeparatorIndex >= 0 && decimalSeparatorIndex == normalized.Length - 1)
        {
            return false;
        }

        return decimal.TryParse(
            normalized,
            NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out result);
    }
}
