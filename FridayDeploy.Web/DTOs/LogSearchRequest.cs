namespace FridayDeploy.Web.DTOs;

public sealed class LogSearchRequest
{
    public string? Query { get; set; }
    public string? Application { get; set; }
    public string? Environment { get; set; }
    public string? Level { get; set; }
    public string? Machine { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? ContainsText { get; set; }
    public string? Source { get; set; }
    public string? PropertyName { get; set; }
    public string? PropertyValue { get; set; }
    public DateTime? FromUtc { get; set; }
    public DateTime? ToUtc { get; set; }
    public double? MinDuration { get; set; }
    public double? MaxDuration { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string Sort { get; set; } = "timestamp_desc";
}

public sealed record SearchFilter(string Field, string Operator, string Value);
