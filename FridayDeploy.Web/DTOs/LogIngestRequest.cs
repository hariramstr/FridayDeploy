using System.Text.Json;
using System.Text.Json.Serialization;

namespace FridayDeploy.Web.DTOs;

public sealed class LogIngestRequest
{
    public DateTime? TimestampUtc { get; set; }
    public required string Application { get; set; }
    public string? Environment { get; set; }
    public string? Version { get; set; }
    public string? Machine { get; set; }
    public required string Level { get; set; }
    public required string Message { get; set; }
    public string? Exception { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? RequestId { get; set; }
    public string? CorrelationId { get; set; }
    public string? ThreadId { get; set; }
    public double? Duration { get; set; }
    [JsonPropertyName("ipAddress")]
    public string? IPAddress { get; set; }
    public Dictionary<string, JsonElement>? Properties { get; set; }
}
