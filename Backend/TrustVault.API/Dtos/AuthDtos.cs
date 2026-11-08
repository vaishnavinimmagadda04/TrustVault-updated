using System.ComponentModel.DataAnnotations;

namespace TrustVault.API.Dtos;

public sealed record RegisterRequest(
    [Required, StringLength(100, MinimumLength = 1)] string Name,
    [Required, EmailAddress, StringLength(254)] string Email,
    [Required, MinLength(8), StringLength(128)] string Password,
    [Required] string Role);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(int UserId, string Token, string Name, string Email, string Role);

public sealed record DocumentResponse(
    int DocumentId,
    string Title,
    string Category,
    string OriginalFileName,
    DateTime UploadDate,
    int OwnerId,
    string OwnerName,
    string AccessLevel = "OWNER");
