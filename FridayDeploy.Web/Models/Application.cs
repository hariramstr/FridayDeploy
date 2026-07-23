namespace FridayDeploy.Web.Models;

public sealed class Application
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public DateTime FirstSeenUtc { get; set; }
    public DateTime LastSeenUtc { get; set; }
    public List<ApiKey> ApiKeys { get; set; } = [];
}
