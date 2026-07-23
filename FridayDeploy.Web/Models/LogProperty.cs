using System.Text.Json.Serialization;

namespace FridayDeploy.Web.Models;

public sealed class LogProperty
{
    public long Id { get; set; }
    public long LogId { get; set; }

    [JsonIgnore]
    public Log Log { get; set; } = null!;

    public required string PropertyName { get; set; }
    public string? PropertyValue { get; set; }
    public required string PropertyType { get; set; }
}
