using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrustVault.API.Data;
using TrustVault.API.Dtos;
using TrustVault.API.Models;
using TrustVault.API.Services;

namespace TrustVault.API.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(AppDbContext db, JwtTokenService tokens, DocumentAccessService access) : ControllerBase
{
    private static readonly string[] SelfAssignableRoles = ["Owner", "Viewer"];

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (!SelfAssignableRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Choose Owner or Viewer. Admin accounts must be provisioned by an administrator." });
        }
        if (await db.Users.AnyAsync(user => user.Email == email))
        {
            return Conflict(new { message = "An account with this email already exists." });
        }

        var user = new AppUser
        {
            Name = request.Name.Trim(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = SelfAssignableRoles.First(role => role.Equals(request.Role, StringComparison.OrdinalIgnoreCase))
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Created("/api/auth/login", new { user.UserId, user.Name, user.Email, user.Role });
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Email == email);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            await access.RecordAsync(user?.UserId, null, "LOGIN", false, "Invalid credentials");
            return Unauthorized(new { message = "Email or password is incorrect." });
        }
        await access.RecordAsync(user.UserId, null, "LOGIN", true, "Login succeeded");
        return Ok(new AuthResponse(user.UserId, tokens.CreateToken(user), user.Name, user.Email, user.Role));
    }
}
