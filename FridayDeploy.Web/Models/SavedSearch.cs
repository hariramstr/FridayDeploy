namespace FridayDeploy.Web.Models;

public sealed class SavedSearch
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public required string Name { get; set; }

    /// <summary>The raw query string (e.g. "?Application=Foo&amp;Level=Error") used to reapply this search directly against /Logs.</summary>
    public required string QueryString { get; set; }
    public DateTime CreatedUtc { get; set; }
}
