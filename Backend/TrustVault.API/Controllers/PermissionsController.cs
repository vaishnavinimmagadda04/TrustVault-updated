using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrustVault.API.Data;
using TrustVault.API.Dtos;
using TrustVault.API.Models;
using TrustVault.API.Services;

namespace TrustVault.API.Controllers;

[ApiController]
[Authorize]
[Route("api/permissions")]
public sealed class PermissionsController(AppDbContext db, DocumentAccessService access) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] int documentId)
    {
        var document = await GetManageableDocument(documentId);
        if (document.Result is not null) return document.Result;
        var users = await db.Users.Where(user => user.UserId != document.Document!.OwnerId)
            .OrderBy(user => user.Name)
            .Select(user => new UserOptionResponse(user.UserId, user.Name, user.Email))
            .ToListAsync();
        return Ok(users);
    }

    [HttpGet("document/{documentId:int}")]
    public async Task<IActionResult> List(int documentId)
    {
        var document = await GetManageableDocument(documentId);
        if (document.Result is not null) return document.Result;
        var permissions = await db.DocumentPermissions.Include(permission => permission.User)
            .Where(permission => permission.DocumentId == documentId)
            .OrderBy(permission => permission.User!.Name)
            .Select(permission => new PermissionResponse(permission.PermissionId, permission.DocumentId,
                permission.UserId, permission.User!.Name, permission.User.Email, permission.PermissionType,
                permission.GrantedBy, permission.GrantedDate, permission.IsActive))
            .ToListAsync();
        return Ok(permissions);
    }

    [HttpPost("grant")]
    public async Task<IActionResult> Grant(GrantPermissionRequest request)
    {
        var permissionType = request.Permission.Trim().ToUpperInvariant();
        if (permissionType is not ("READ" or "EDIT"))
        {
            return BadRequest(new { message = "Permission must be READ or EDIT." });
        }
        var document = await GetManageableDocument(request.DocumentId);
        if (document.Result is not null) return document.Result;
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.UserId == request.UserId);
        if (user is null) return NotFound(new { message = "User not found." });
        if (user.UserId == document.Document!.OwnerId)
        {
            return BadRequest(new { message = "The document owner already has full access." });
        }

        var permission = await db.DocumentPermissions.FirstOrDefaultAsync(item =>
            item.DocumentId == request.DocumentId && item.UserId == request.UserId);
        if (permission is null)
        {
            permission = new DocumentPermission
            {
                DocumentId = request.DocumentId,
                UserId = request.UserId,
                GrantedBy = CurrentUserId,
                GrantedDate = DateTime.UtcNow,
                PermissionType = permissionType,
                IsActive = true
            };
            db.DocumentPermissions.Add(permission);
        }
        else
        {
            permission.PermissionType = permissionType;
            permission.GrantedBy = CurrentUserId;
            permission.GrantedDate = DateTime.UtcNow;
            permission.IsActive = true;
        }
        await db.SaveChangesAsync();
        await access.RecordAsync(CurrentUserId, request.DocumentId, "ACCESS_GRANTED", true,
            $"{document.Document!.Title}: {permissionType} granted to {user.Email}");
        return Ok(new { permission.PermissionId, permission.DocumentId, permission.UserId, permission.PermissionType, permission.IsActive });
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke(RevokePermissionRequest request)
    {
        var document = await GetManageableDocument(request.DocumentId);
        if (document.Result is not null) return document.Result;
        var permission = await db.DocumentPermissions.FirstOrDefaultAsync(item =>
            item.DocumentId == request.DocumentId && item.UserId == request.UserId && item.IsActive);
        if (permission is null) return NotFound(new { message = "No active permission exists for this user." });
        permission.IsActive = false;
        await db.SaveChangesAsync();
        var userEmail = await db.Users.Where(user => user.UserId == request.UserId).Select(user => user.Email).SingleOrDefaultAsync();
        await access.RecordAsync(CurrentUserId, request.DocumentId, "ACCESS_REVOKED", true,
            $"{document.Document!.Title}: access revoked from {userEmail ?? request.UserId.ToString()}");
        return NoContent();
    }

    private async Task<(Document? Document, IActionResult? Result)> GetManageableDocument(int documentId)
    {
        var document = await db.Documents.SingleOrDefaultAsync(item => item.DocumentId == documentId);
        if (document is null) return (null, NotFound());
        if (!User.IsInRole("Admin") && document.OwnerId != CurrentUserId)
        {
            await access.RecordAsync(CurrentUserId, documentId, "ACCESS_DENIED", false,
                $"MANAGE_PERMISSIONS denied: {document.Title}");
            return (null, StatusCode(StatusCodes.Status403Forbidden,
                new { message = "Only the document owner or an administrator can manage access." }));
        }
        return (document, null);
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
