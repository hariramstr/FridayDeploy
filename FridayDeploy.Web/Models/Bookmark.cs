namespace FridayDeploy.Web.Models;

public sealed class Bookmark
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public long? LogId { get; set; }
    public string? CorrelationId { get; set; }
    public string? RequestId { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedUtc { get; set; }
}
