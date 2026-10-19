using System.Globalization;

namespace FarmaciaSolidariaCristiana.Helpers;

public static class CubaTime
{
    public const string TimePattern = "h:mm tt";
    public const string DateTimePattern = "dd/MM/yyyy h:mm tt";
    public const string ShortDateTimePattern = "dd/MM h:mm tt";

    private static readonly TimeZoneInfo Zone = ResolveZone();

    public static TimeZoneInfo Havana => Zone;

    public static DateTime Now => ToCuba(DateTime.UtcNow);

    public static DateTime Today => Now.Date;

    public static DateTime ToCuba(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zone);

    public static string FormatTime(DateTime value) => value.ToString(TimePattern, CultureInfo.InvariantCulture);
    public static string? FormatTime(DateTime? value) => value?.ToString(TimePattern, CultureInfo.InvariantCulture);
    public static string FormatDateTime(DateTime value) => value.ToString(DateTimePattern, CultureInfo.InvariantCulture);
    public static string? FormatDateTime(DateTime? value) => value?.ToString(DateTimePattern, CultureInfo.InvariantCulture);
    public static string FormatShortDateTime(DateTime value) => value.ToString(ShortDateTimePattern, CultureInfo.InvariantCulture);
    public static string? FormatShortDateTime(DateTime? value) => value?.ToString(ShortDateTimePattern, CultureInfo.InvariantCulture);

    private static TimeZoneInfo ResolveZone()
    {
        foreach (var id in new[] { "America/Havana", "Cuba Standard Time" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }
        throw new InvalidOperationException(
            "No se pudo resolver la zona horaria de Cuba (America/Havana). Verifica la globalización (ICU) del host.");
    }
}
