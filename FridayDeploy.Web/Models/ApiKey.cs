namespace FridayDeploy.Web.Models;

public sealed class ApiKey
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public Application Application { get; set; } = null!;

    /// <summary>SHA-256 hash (hex) of the raw key. API keys are high-entropy random secrets, so a fast
    /// deterministic hash — rather than a slow salted hash like BCrypt — is appropriate and allows an
    /// indexed lookup instead of scanning every key on each request.</summary>
    public required string KeyHash { get; set; }

    /// <summary>First characters of the raw key, kept for display/identification purposes only.</summary>
    public required string Prefix { get; set; }

    public DateTime CreatedUtc { get; set; }
    public DateTime? LastUsedUtc { get; set; }
    public bool IsDisabled { get; set; }
}
