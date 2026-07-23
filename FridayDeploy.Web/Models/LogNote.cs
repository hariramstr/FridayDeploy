namespace FridayDeploy.Web.Models;

public sealed class LogNote
{
    public int Id { get; set; }
    public long LogId { get; set; }
    public required string Author { get; set; }
    public required string Body { get; set; }
    public DateTime CreatedUtc { get; set; }
}
