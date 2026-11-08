using Microsoft.EntityFrameworkCore;
using TrustVault.API.Models;

namespace TrustVault.API.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentPermission> DocumentPermissions => Set<DocumentPermission>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(user => user.UserId);
            entity.Property(user => user.Name).HasMaxLength(100).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(254).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(user => user.Role).HasMaxLength(20).IsRequired();
        });
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(document => document.DocumentId);
            entity.Property(document => document.Title).HasMaxLength(200).IsRequired();
            entity.Property(document => document.Category).HasMaxLength(100).IsRequired();
            entity.Property(document => document.FileName).HasMaxLength(255).IsRequired();
            entity.Property(document => document.StoredFileName).HasMaxLength(100).IsRequired();
            entity.Property(document => document.OriginalFileName).HasMaxLength(255).IsRequired();
            entity.HasOne(document => document.Owner).WithMany(user => user.Documents)
                .HasForeignKey(document => document.OwnerId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<DocumentPermission>(entity =>
        {
            entity.ToTable("DocumentPermissions");
            entity.HasKey(permission => permission.PermissionId);
            entity.Property(permission => permission.PermissionType).HasMaxLength(10).IsRequired();
            entity.Property(permission => permission.IsActive).HasDefaultValue(true);
            entity.HasIndex(permission => new { permission.DocumentId, permission.UserId });
            entity.HasOne(permission => permission.Document).WithMany().HasForeignKey(permission => permission.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(permission => permission.User).WithMany().HasForeignKey(permission => permission.UserId)
                .OnDelete(DeleteBehavior.NoAction);
            entity.HasOne(permission => permission.GrantingUser).WithMany().HasForeignKey(permission => permission.GrantedBy)
                .OnDelete(DeleteBehavior.NoAction);
        });
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("AuditLogs");
            entity.HasKey(log => log.LogId);
            entity.Property(log => log.Action).HasMaxLength(40).IsRequired();
            entity.Property(log => log.IpAddress).HasMaxLength(45);
            entity.Property(log => log.Details).HasMaxLength(1000);
            entity.Property(log => log.Timestamp).HasDefaultValueSql("SYSUTCDATETIME()");
            entity.HasIndex(log => log.Timestamp);
            entity.HasOne(log => log.User).WithMany().HasForeignKey(log => log.UserId).OnDelete(DeleteBehavior.NoAction);
        });
    }
}
