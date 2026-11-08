namespace TrustVault.API.Models;

public sealed class Document
{
    public int DocumentId { get; set; }
    public required string Title { get; set; }
    public required string Category { get; set; }
    public required string FileName { get; set; }
    public required string StoredFileName { get; set; }
    public required string OriginalFileName { get; set; }
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    public int OwnerId { get; set; }
    public AppUser? Owner { get; set; }
}
