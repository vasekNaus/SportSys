using System.Globalization;

namespace SportSys.Razor.Infrastructure;

public static class CzechDateTimeFormat
{
    public const string DateFormat = "d. M. yyyy";
    public const string TimeFormat = "HH:mm";

    private static readonly string[] DateFormats =
    [
        "d.M.yyyy",
        "d. M. yyyy",
        "dd.MM.yyyy",
        "dd. MM. yyyy",
        "yyyy-MM-dd",
    ];

    private static readonly string[] TimeFormats =
    [
        "H:mm",
        "HH:mm",
        "H:mm:ss",
        "HH:mm:ss",
    ];

    public static string FormatDate(object? value) => value switch
    {
        DateOnly date => date.ToString(DateFormat, CultureInfo.InvariantCulture),
        DateTime dateTime => dateTime.ToString(DateFormat, CultureInfo.InvariantCulture),
        _ => string.Empty,
    };

    public static string FormatTime(object? value) => value switch
    {
        TimeOnly time => time.ToString(TimeFormat, CultureInfo.InvariantCulture),
        TimeSpan time => time.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
        _ => string.Empty,
    };

    public static bool TryParseDate(string value, out DateOnly result)
        => DateOnly.TryParseExact(
            value,
            DateFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);

    public static bool TryParseTime(string value, out TimeOnly result)
        => TimeOnly.TryParseExact(
            value,
            TimeFormats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
}
