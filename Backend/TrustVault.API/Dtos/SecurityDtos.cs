using System.ComponentModel.DataAnnotations;

namespace TrustVault.API.Dtos;

public sealed record GrantPermissionRequest(
    [Range(1, int.MaxValue)] int DocumentId,
    [Range(1, int.MaxValue)] int UserId,
    [Required] string Permission);

public sealed record RevokePermissionRequest(
    [Range(1, int.MaxValue)] int DocumentId,
    [Range(1, int.MaxValue)] int UserId);

public sealed record EditDocumentRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    [Required, StringLength(100, MinimumLength = 1)] string Category);

public sealed record UserOptionResponse(int UserId, string Name, string Email);

public sealed record PermissionResponse(
    int PermissionId,
    int DocumentId,
    int UserId,
    string UserName,
    string Email,
    string Permission,
    int GrantedBy,
    DateTime GrantedDate,
    bool IsActive);

public sealed record ActivityResponse(
    long LogId,
    int? UserId,
    string UserName,
    int? DocumentId,
    string DocumentTitle,
    string Action,
    bool WasAllowed,
    DateTime Timestamp,
    string? IpAddress,
    string? Details);
