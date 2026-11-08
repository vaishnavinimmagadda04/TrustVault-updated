namespace TrustVault.API.Models;

public sealed class AppUser
{
    public int UserId { get; set; }
    public required string Name { get; set; }
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string Role { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public List<Document> Documents { get; set; } = [];
}
