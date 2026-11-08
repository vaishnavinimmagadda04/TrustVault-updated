namespace TrustVault.API.Models;

public sealed class AuditLog
{
    public long LogId { get; set; }
    public int? UserId { get; set; }
    public int? DocumentId { get; set; }
    public required string Action { get; set; }
    public bool WasAllowed { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? Details { get; set; }
    public AppUser? User { get; set; }
}