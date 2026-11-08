using System.ComponentModel.DataAnnotations;
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
[Route("api/document")]
public sealed class DocumentsController(
    AppDbContext db,
    DocumentAccessService access,
    IWebHostEnvironment environment,
    IConfiguration configuration) : ControllerBase
{
    private const long MaxFileBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".docx", ".xlsx" };

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> List()
    {
        var userId = CurrentUserId;
        var isAdmin = User.IsInRole("Admin");
        var query = AccessibleDocuments(userId, isAdmin);
        var documents = await query.OrderByDescending(document => document.UploadDate)
            .Select(document => new DocumentResponse(document.DocumentId, document.Title, document.Category,
                document.OriginalFileName, document.UploadDate, document.OwnerId, document.Owner!.Name,
                isAdmin ? "ADMIN" : document.OwnerId == userId ? "OWNER" :
                    db.DocumentPermissions.Where(permission => permission.DocumentId == document.DocumentId &&
                        permission.UserId == userId && permission.IsActive)
                        .OrderByDescending(permission => permission.PermissionType == "EDIT")
                        .Select(permission => permission.PermissionType).FirstOrDefault() ?? "READ"))
            .ToListAsync();
        return Ok(documents);
    }

    [HttpGet("search")]
    public async Task<ActionResult<IReadOnlyList<DocumentResponse>>> Search(
        [FromQuery, StringLength(200)] string? name,
        [FromQuery, StringLength(100)] string? owner,
        [FromQuery, StringLength(100)] string? category,
        [FromQuery] DateOnly? date)
    {
        var userId = CurrentUserId;
        var isAdmin = User.IsInRole("Admin");
        var query = AccessibleDocuments(userId, isAdmin);

        if (!string.IsNullOrWhiteSpace(name))
        {
            var value = name.Trim();
            query = query.Where(document => document.Title.Contains(value) ||
                document.OriginalFileName.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(owner))
        {
            var value = owner.Trim();
            query = query.Where(document => document.Owner!.Name.Contains(value) ||
                document.Owner.Email.Contains(value));
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            var value = category.Trim();
            query = query.Where(document => document.Category.Contains(value));
        }
        if (date is { } uploadDate)
        {
            var start = uploadDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var end = start.AddDays(1);
            query = query.Where(document => document.UploadDate >= start && document.UploadDate < end);
        }

        var documents = await query.OrderByDescending(document => document.UploadDate)
            .Select(document => new DocumentResponse(document.DocumentId, document.Title, document.Category,
                document.OriginalFileName, document.UploadDate, document.OwnerId, document.Owner!.Name,
                isAdmin ? "ADMIN" : document.OwnerId == userId ? "OWNER" :
                    db.DocumentPermissions.Where(permission => permission.DocumentId == document.DocumentId &&
                        permission.UserId == userId && permission.IsActive)
                        .OrderByDescending(permission => permission.PermissionType == "EDIT")
                        .Select(permission => permission.PermissionType).FirstOrDefault() ?? "READ"))
            .ToListAsync();
        return Ok(documents);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(11 * 1024 * 1024)]
    public async Task<ActionResult<DocumentResponse>> Upload([FromForm] string title, [FromForm] string category, [FromForm] IFormFile file)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Owner"))
        {
            await access.RecordAsync(CurrentUserId, null, "ACCESS_DENIED", false, "UPLOAD");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to upload documents." });
        }
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 200 ||
            string.IsNullOrWhiteSpace(category) || category.Trim().Length > 100)
        {
            return BadRequest(new { message = "Title (1-200 characters) and category (1-100 characters) are required." });
        }
        if (file is null || file.Length == 0)
        {
            return BadRequest(new { message = "Choose a non-empty PDF, DOCX, or XLSX file." });
        }
        if (file.Length > MaxFileBytes)
        {
            return BadRequest(new { message = "Files must be 10 MB or smaller." });
        }
        var extension = Path.GetExtension(Path.GetFileName(file.FileName));
        if (!AllowedExtensions.Contains(extension) || !await HasExpectedSignature(file, extension))
        {
            return BadRequest(new { message = "Only valid PDF, DOCX, and XLSX files are accepted." });
        }

        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var uploadDirectory = GetUploadDirectory();
        Directory.CreateDirectory(uploadDirectory);
        var destination = Path.Combine(uploadDirectory, storedFileName);
        await using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream);
        }

        var document = new Document
        {
            Title = title.Trim(),
            Category = category.Trim(),
            FileName = storedFileName,
            StoredFileName = storedFileName,
            OriginalFileName = Path.GetFileName(file.FileName),
            OwnerId = CurrentUserId,
            UploadDate = DateTime.UtcNow
        };
        db.Documents.Add(document);
        try
        {
            await db.SaveChangesAsync();
        }
        catch
        {
            db.Entry(document).State = EntityState.Detached;
            if (System.IO.File.Exists(destination)) System.IO.File.Delete(destination);
            await access.RecordAsync(CurrentUserId, null, "UPLOAD", false, "Upload could not be stored.");
            throw;
        }
        await access.RecordAsync(CurrentUserId, document.DocumentId, "UPLOAD", true, document.Title);
        var ownerName = User.FindFirstValue(ClaimTypes.Name) ?? "";
        return CreatedAtAction(nameof(View), new { id = document.DocumentId },
            new DocumentResponse(document.DocumentId, document.Title, document.Category,
                document.OriginalFileName, document.UploadDate, document.OwnerId, ownerName));
    }

    [HttpGet("{id:int}")]
    [HttpGet("{id:int}/view")]
    public async Task<IActionResult> View(int id)
    {
        var check = await access.CheckAsync(id, CurrentUserId, CurrentRole, "READ");
        if (check.Document is null) return NotFound();
        if (!check.Allowed) return await Denied(id, "VIEW");
        var path = Path.Combine(GetUploadDirectory(), check.Document.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound(new { message = "The stored file is missing." });
        await access.RecordAsync(CurrentUserId, id, "VIEW", true, check.Document.Title);
        return PhysicalFile(path, GetContentType(check.Document.StoredFileName), enableRangeProcessing: true);
    }

    [HttpGet("download/{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var check = await access.CheckAsync(id, CurrentUserId, CurrentRole, "READ");
        if (check.Document is null) return NotFound();
        if (!check.Allowed) return await Denied(id, "DOWNLOAD");
        var path = Path.Combine(GetUploadDirectory(), check.Document.StoredFileName);
        if (!System.IO.File.Exists(path)) return NotFound(new { message = "The stored file is missing." });
        await access.RecordAsync(CurrentUserId, id, "DOWNLOAD", true, check.Document.Title);
        return PhysicalFile(path, GetContentType(check.Document.StoredFileName), check.Document.OriginalFileName,
            enableRangeProcessing: true);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, EditDocumentRequest request)
    {
        var check = await access.CheckAsync(id, CurrentUserId, CurrentRole, "EDIT");
        if (check.Document is null) return NotFound();
        if (!check.Allowed) return await Denied(id, "EDIT");
        check.Document.Title = request.Title.Trim();
        check.Document.Category = request.Category.Trim();
        await db.SaveChangesAsync();
        await access.RecordAsync(CurrentUserId, id, "EDIT", true, check.Document.Title);
        return Ok(new { check.Document.DocumentId, check.Document.Title, check.Document.Category });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var document = await db.Documents.SingleOrDefaultAsync(item => item.DocumentId == id);
        if (document is null) return NotFound();
        if (!User.IsInRole("Admin") && document.OwnerId != CurrentUserId)
        {
            return await Denied(id, "DELETE");
        }
        db.Documents.Remove(document);
        await db.SaveChangesAsync();
        var path = Path.Combine(GetUploadDirectory(), document.StoredFileName);
        if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        await access.RecordAsync(CurrentUserId, id, "DELETE", true, document.Title);
        return NoContent();
    }

    private async Task<IActionResult> Denied(int documentId, string attemptedAction)
    {
        var title = await db.Documents.Where(document => document.DocumentId == documentId)
            .Select(document => document.Title).SingleOrDefaultAsync();
        await access.RecordAsync(CurrentUserId, documentId, "ACCESS_DENIED", false,
            $"{attemptedAction} denied: {title ?? "Unknown document"}");
        return StatusCode(StatusCodes.Status403Forbidden, new { message = "You do not have permission to access this document." });
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private string CurrentRole => User.FindFirstValue(ClaimTypes.Role) ?? "";

    private IQueryable<Document> AccessibleDocuments(int userId, bool isAdmin)
    {
        var query = db.Documents.Include(document => document.Owner).AsQueryable();
        if (!isAdmin)
        {
            query = query.Where(document => document.OwnerId == userId || db.DocumentPermissions.Any(permission =>
                permission.DocumentId == document.DocumentId && permission.UserId == userId && permission.IsActive));
        }
        return query;
    }

    private string GetUploadDirectory() => Path.GetFullPath(configuration["Storage:UploadDirectory"] ??
        Path.Combine(environment.ContentRootPath, "Uploads"));

    private static string GetContentType(string fileName) => Path.GetExtension(fileName).ToLowerInvariant() switch
    {
        ".pdf" => "application/pdf",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };

    private static async Task<bool> HasExpectedSignature(IFormFile file, string extension)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header);
        if (extension.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return read >= 5 && header.AsSpan(0, 5).SequenceEqual("%PDF-"u8);
        }
        return read >= 4 && header[0] == 0x50 && header[1] == 0x4B && header[2] is 0x03 or 0x05 or 0x07 && header[3] is 0x04 or 0x06 or 0x08;
    }
}
