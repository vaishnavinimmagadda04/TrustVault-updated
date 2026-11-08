using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrustVault.API.Data;
using TrustVault.API.Dtos;

namespace TrustVault.API.Controllers;

[ApiController]
[Authorize]
[Route("api/dashboard")]
public sealed class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var query = db.Documents.Include(document => document.Owner).AsQueryable();
        if (!User.IsInRole("Admin")) query = query.Where(document => document.OwnerId == currentUserId);
        var total = await query.CountAsync();
        var myUploads = await query.CountAsync(document => document.OwnerId == currentUserId);
        var recent = await query.OrderByDescending(document => document.UploadDate).Take(5)
            .Select(document => new DocumentResponse(document.DocumentId, document.Title, document.Category,
                document.OriginalFileName, document.UploadDate, document.OwnerId, document.Owner!.Name,
                User.IsInRole("Admin") ? "ADMIN" : "OWNER"))
            .ToListAsync();
        return Ok(new { totalDocuments = total, myUploads, recentDocuments = recent });
    }
}
