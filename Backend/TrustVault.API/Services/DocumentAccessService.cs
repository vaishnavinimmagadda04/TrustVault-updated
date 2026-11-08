using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using TrustVault.API.Data;
using TrustVault.API.Models;

namespace TrustVault.API.Services;

public sealed record DocumentAccessResult(Document? Document, bool Allowed, string? PermissionType);

public sealed class DocumentAccessService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
{
    public async Task<DocumentAccessResult> CheckAsync(int documentId, int userId, string role, string requiredPermission)
    {
        var document = await db.Documents.SingleOrDefaultAsync(item => item.DocumentId == documentId);
        if (document is null) return new DocumentAccessResult(null, false, null);
        if (role == "Admin") return new DocumentAccessResult(document, true, "ADMIN");
        if (document.OwnerId == userId) return new DocumentAccessResult(document, true, "OWNER");

        var permission = await db.DocumentPermissions
            .Where(item => item.DocumentId == documentId && item.UserId == userId && item.IsActive)
            .OrderByDescending(item => item.PermissionType == "EDIT")
            .Select(item => item.PermissionType)
            .FirstOrDefaultAsync();
        var allowed = permission == "EDIT" || (requiredPermission == "READ" && permission == "READ");
        return new DocumentAccessResult(document, allowed, permission);
    }

    public async Task RecordAsync(int? userId, int? documentId, string action, bool wasAllowed, string? details = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            DocumentId = documentId,
            Action = action,
            WasAllowed = wasAllowed,
            Timestamp = DateTime.UtcNow,
            IpAddress = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            Details = details
        });
        await db.SaveChangesAsync();
    }

    public int? CurrentUserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }
}
