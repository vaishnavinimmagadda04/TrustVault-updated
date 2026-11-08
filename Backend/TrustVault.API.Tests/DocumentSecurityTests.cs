using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.TestHost;
using TrustVault.API.Data;
using TrustVault.API.Dtos;
using TrustVault.API.Models;
using TrustVault.API.Services;

namespace TrustVault.API.Tests;

public sealed class DocumentSecurityTests
{
    [Fact]
    public async Task GranteeCanViewUntilAccessIsRevokedAndDenialsAreAudited()
    {
        using var factory = new TestApiFactory();
        await factory.SeedAsync();
        using var owner = factory.CreateAuthenticatedClient(1, "Owner");
        using var viewer = factory.CreateAuthenticatedClient(2, "Viewer");

        var deniedBeforeGrant = await viewer.GetAsync("/api/document/41/view");
        Assert.Equal(HttpStatusCode.Forbidden, deniedBeforeGrant.StatusCode);

        var grant = await owner.PostAsJsonAsync("/api/permissions/grant",
            new { documentId = 41, userId = 2, permission = "READ" });
        Assert.Equal(HttpStatusCode.OK, grant.StatusCode);

        var allowed = await viewer.GetAsync("/api/document/41/view");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        var revoke = await owner.PostAsJsonAsync("/api/permissions/revoke",
            new { documentId = 41, userId = 2 });
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        var deniedAfterRevoke = await viewer.GetAsync("/api/document/41/view");
        Assert.Equal(HttpStatusCode.Forbidden, deniedAfterRevoke.StatusCode);

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var denials = await db.AuditLogs.CountAsync(log =>
            log.UserId == 2 && log.Action == "ACCESS_DENIED" && !log.WasAllowed);
        Assert.Equal(2, denials);
    }

    [Fact]
    public async Task ViewerCannotAccessAdminActivity()
    {
        using var factory = new TestApiFactory();
        await factory.SeedAsync();
        using var viewer = factory.CreateAuthenticatedClient(2, "Viewer");

        var response = await viewer.GetAsync("/api/admin/activity");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SearchAppliesAllFiltersWithoutExposingOtherUsersDocuments()
    {
        using var factory = new TestApiFactory();
        await factory.SeedAsync();
        using var owner = factory.CreateAuthenticatedClient(1, "Owner");
        using var viewer = factory.CreateAuthenticatedClient(2, "Viewer");
        const string date = "2026-11-06";

        foreach (var filter in new[] { "name=Private", "owner=Document%20Owner", "category=Finance", $"date={date}" })
        {
            var matches = await owner.GetFromJsonAsync<List<DocumentResponse>>($"/api/document/search?{filter}");
            Assert.NotNull(matches);
            Assert.Single(matches);
            Assert.Equal(41, matches[0].DocumentId);
        }

        var hiddenDocuments = await viewer.GetFromJsonAsync<List<DocumentResponse>>("/api/document/search?name=Budget");
        Assert.NotNull(hiddenDocuments);
        Assert.Empty(hiddenDocuments);
    }

    [Fact]
    public async Task UploadRejectsDisallowedExtension()
    {
        using var factory = new TestApiFactory();
        await factory.SeedAsync();
        using var owner = factory.CreateAuthenticatedClient(1, "Owner");
        using var content = CreateUpload("malware.exe", [0x4d, 0x5a]);

        var response = await owner.PostAsync("/api/document/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadRejectsFilesLargerThanTenMiB()
    {
        using var factory = new TestApiFactory();
        await factory.SeedAsync();
        using var owner = factory.CreateAuthenticatedClient(1, "Owner");
        var bytes = new byte[10 * 1024 * 1024 + 1];
        "%PDF-"u8.CopyTo(bytes);
        using var content = CreateUpload("large.pdf", bytes);

        var response = await owner.PostAsync("/api/document/upload", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadAcceptsValidPdfUnderLimit()
    {
        using var factory = new TestApiFactory();
        await factory.SeedAsync();
        using var owner = factory.CreateAuthenticatedClient(1, "Owner");
        using var content = CreateUpload("report.pdf", Encoding.ASCII.GetBytes("%PDF-1.7\nsample"));

        var response = await owner.PostAsync("/api/document/upload", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static MultipartFormDataContent CreateUpload(string fileName, byte[] bytes)
    {
        var content = new MultipartFormDataContent
        {
            { new StringContent("Test document"), "title" },
            { new StringContent("Finance"), "category" }
        };
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        content.Add(file, "file", fileName);
        return content;
    }

    private sealed class TestApiFactory : WebApplicationFactory<Program>
    {
        private readonly string databaseName = $"TrustVaultTests-{Guid.NewGuid():N}";
        private readonly string uploadDirectory = Path.Combine(Path.GetTempPath(), $"TrustVaultTests-{Guid.NewGuid():N}");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("Jwt:Key", "test-key-for-trustvault-integration-tests-only");
            builder.UseSetting("Jwt:Issuer", "TrustVault.Tests");
            builder.UseSetting("Jwt:Audience", "TrustVault.Tests");
            builder.UseSetting("Storage:UploadDirectory", uploadDirectory);
            builder.UseSetting("FrontendOrigin", "http://localhost");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] = "test-key-for-trustvault-integration-tests-only",
                    ["Jwt:Issuer"] = "TrustVault.Tests",
                    ["Jwt:Audience"] = "TrustVault.Tests",
                    ["Storage:UploadDirectory"] = uploadDirectory,
                    ["FrontendOrigin"] = "http://localhost"
                }));
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<AppDbContext>();
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
            });
        }

        public async Task SeedAsync()
        {
            Directory.CreateDirectory(uploadDirectory);
            await File.WriteAllBytesAsync(Path.Combine(uploadDirectory, "secured.pdf"), "%PDF-1.7\nprivate content"u8.ToArray());
            await using var scope = Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();
            db.Users.AddRange(
                new AppUser { UserId = 1, Name = "Document Owner", Email = "owner@example.test", PasswordHash = "not-used", Role = "Owner" },
                new AppUser { UserId = 2, Name = "Document Viewer", Email = "viewer@example.test", PasswordHash = "not-used", Role = "Viewer" },
                new AppUser { UserId = 3, Name = "Other Owner", Email = "other@example.test", PasswordHash = "not-used", Role = "Owner" });
            db.Documents.AddRange(
                new Document
                {
                    DocumentId = 41,
                    Title = "Private report",
                    Category = "Finance",
                    FileName = "secured.pdf",
                    StoredFileName = "secured.pdf",
                    OriginalFileName = "report.pdf",
                    OwnerId = 1,
                    UploadDate = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc)
                },
                new Document
                {
                    DocumentId = 42,
                    Title = "Budget planning",
                    Category = "Finance",
                    FileName = "other.pdf",
                    StoredFileName = "other.pdf",
                    OriginalFileName = "budget.pdf",
                    OwnerId = 3,
                    UploadDate = new DateTime(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc)
                });
            await db.SaveChangesAsync();
        }

        public HttpClient CreateAuthenticatedClient(int userId, string role)
        {
            var client = CreateClient();
            var configuration = Services.GetRequiredService<IConfiguration>();
            var user = new AppUser
            {
                UserId = userId,
                Name = $"Test {role}",
                Email = $"test-{userId}@example.test",
                PasswordHash = "not-used",
                Role = role
            };
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", new JwtTokenService(configuration).CreateToken(user));
            return client;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && Directory.Exists(uploadDirectory))
            {
                Directory.Delete(uploadDirectory, recursive: true);
            }
        }
    }
}
