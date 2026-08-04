using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FridayDeploy.Web.Helpers;

/// <summary>Groups occurrences of "the same bug" together even though exception messages carry
/// per-occurrence data (ids, counts, timestamps) and stack traces carry line numbers that shift between
/// builds. Works from the existing Log.Exception/Log.StackTrace fields — no ingestion changes required.</summary>
public static partial class ExceptionFingerprintHelper
{
    private const int MaxStackFrames = 3;

    public static string Compute(string? exceptionMessage, string? stackTrace)
    {
        var normalized = NormalizeMessage(exceptionMessage) + "|" + NormalizeStackFrames(stackTrace);
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string NormalizeMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return "";
        }

        var withoutGuids = GuidPattern().Replace(message, "{guid}");
        return DigitPattern().Replace(withoutGuids, "{n}");
    }

    private static string NormalizeStackFrames(string? stackTrace)
    {
        if (string.IsNullOrWhiteSpace(stackTrace))
        {
            return "";
        }

        var frames = stackTrace
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(MaxStackFrames)
            .Select(line => LineNumberPattern().Replace(line, ""))
            .Select(line => HexAddressPattern().Replace(line, ""));

        return string.Join('|', frames);
    }

    [GeneratedRegex(@"[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")]
    private static partial Regex GuidPattern();

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitPattern();

    [GeneratedRegex(@":line \d+")]
    private static partial Regex LineNumberPattern();

    [GeneratedRegex(@"0x[0-9a-fA-F]+")]
    private static partial Regex HexAddressPattern();
}
