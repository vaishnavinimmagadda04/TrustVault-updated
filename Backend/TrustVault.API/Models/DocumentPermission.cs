namespace TrustVault.API.Models;

public sealed class DocumentPermission
{
    public int PermissionId { get; set; }
    public int DocumentId { get; set; }
    public int UserId { get; set; }
    public required string PermissionType { get; set; }
    public int GrantedBy { get; set; }
    public DateTime GrantedDate { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public Document? Document { get; set; }
    public AppUser? User { get; set; }
    public AppUser? GrantingUser { get; set; }
}