using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrustVault.API.Data;
using TrustVault.API.Dtos;
using TrustVault.API.Models;

namespace TrustVault.API.Controllers;

[ApiController]
[Authorize(Policy = "AdminOnly")]
[Route("api/admin/activity")]
public sealed class AdminActivityController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var recentUploads = await Project(db.AuditLogs.Where(log => log.Action == "UPLOAD" && log.WasAllowed)
            .OrderByDescending(log => log.Timestamp).Take(20)).ToListAsync();
        var recentDownloads = await Project(db.AuditLogs.Where(log => log.Action == "DOWNLOAD" && log.WasAllowed)
            .OrderByDescending(log => log.Timestamp).Take(20)).ToListAsync();
        var permissionChanges = await Project(db.AuditLogs.Where(log => log.Action == "ACCESS_GRANTED" || log.Action == "ACCESS_REVOKED")
            .OrderByDescending(log => log.Timestamp).Take(20)).ToListAsync();
        var failedAccessAttempts = await Project(db.AuditLogs.Where(log => !log.WasAllowed)
            .OrderByDescending(log => log.Timestamp).Take(50)).ToListAsync();
        return Ok(new { recentUploads, recentDownloads, permissionChanges, failedAccessAttempts });
    }

    private IQueryable<ActivityResponse> Project(IQueryable<AuditLog> logs) => logs.Select(log => new ActivityResponse(
        log.LogId,
        log.UserId,
        log.User == null ? "Unknown user" : log.User.Name,
        log.DocumentId,
        db.Documents.Where(document => document.DocumentId == log.DocumentId)
            .Select(document => document.Title).FirstOrDefault() ??
            (log.DocumentId == null ? "No document" : log.Details ?? "Deleted or unknown document"),
        log.Action,
        log.WasAllowed,
        log.Timestamp,
        log.IpAddress,
        log.Details));
}
