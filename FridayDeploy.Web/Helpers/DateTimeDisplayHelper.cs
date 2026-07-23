using System.Globalization;

namespace FridayDeploy.Web.Helpers;

public static class DateTimeDisplayHelper
{
    private const string Format = "dd-MMM-yyyy hh:mm:ss tt";

    // Invariant culture keeps AM/PM and month abbreviations consistent regardless of the server's locale.
    public static string ToDisplayString(this DateTime value) => value.ToString(Format, CultureInfo.InvariantCulture);

    public static string? ToDisplayString(this DateTime? value) => value?.ToString(Format, CultureInfo.InvariantCulture);
}
