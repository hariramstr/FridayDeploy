namespace FridayDeploy.Web.Models;

/// <summary>A group of Logs that share the same normalized exception message + top stack frames — the same
/// bug firing repeatedly, collapsed into one row instead of hundreds of near-identical ones.</summary>
public sealed class ExceptionFingerprint
{
    public int Id { get; set; }
    public required string Fingerprint { get; set; }
    public required string Application { get; set; }
    public required string SampleMessage { get; set; }
    public string? SampleStackTrace { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public int OccurrenceCount { get; set; }
}
