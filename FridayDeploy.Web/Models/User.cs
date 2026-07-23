namespace FridayDeploy.Web.Models;

public sealed class User
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string PasswordHash { get; set; }
    public bool IsAdministrator { get; set; }
    public DateTime CreatedUtc { get; set; }
}
