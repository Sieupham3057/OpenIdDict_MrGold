using AuthDemo.Api.Models;
using AuthDemo.Api.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthDemo.Api.Data;

// Kế thừa IdentityDbContext để tự động có các bảng Identity:
//   AspNetUsers, AspNetRoles, AspNetUserRoles, AspNetUserClaims...
// Generic params: <User, Role, KeyType>
//   - ApplicationUser: user tùy chỉnh (kế thừa IdentityUser)
//   - IdentityRole<Guid>: dùng Guid làm khóa thay vì string mặc định
//   - Guid: kiểu khóa chính
public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Các bảng của hệ thống phân quyền tùy chỉnh
    public DbSet<AppFunction> Functions => Set<AppFunction>();
    public DbSet<AppAction> Actions => Set<AppAction>();
    public DbSet<ActionInFunction> ActionInFunctions => Set<ActionInFunction>();
    public DbSet<Permission> Permissions => Set<Permission>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Phải gọi base trước để Identity tự cấu hình các bảng AspNet*
        base.OnModelCreating(builder);

        // ─── AppFunction ──────────────────────────────────────────────────────────
        builder.Entity<AppFunction>(e =>
        {
            e.ToTable("Functions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Url).HasMaxLength(500);
            e.Property(x => x.ParentId).HasMaxLength(50);
            e.Property(x => x.Icon).HasMaxLength(100);
        });

        // ─── AppAction ────────────────────────────────────────────────────────────
        builder.Entity<AppAction>(e =>
        {
            e.ToTable("Actions");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(50);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        // ─── ActionInFunction ─────────────────────────────────────────────────────
        // Bảng trung gian: Action nào được phép tồn tại trong Function nào
        // Composite PK: (ActionId, FunctionId) — không cần cột Id riêng
        builder.Entity<ActionInFunction>(e =>
        {
            e.ToTable("ActionInFunctions");
            e.HasKey(x => new { x.ActionId, x.FunctionId });
            e.Property(x => x.ActionId).HasMaxLength(50);
            e.Property(x => x.FunctionId).HasMaxLength(50);

            e.HasOne(x => x.Action)
             .WithMany(a => a.ActionInFunctions)
             .HasForeignKey(x => x.ActionId);

            e.HasOne(x => x.Function)
             .WithMany(f => f.ActionInFunctions)
             .HasForeignKey(x => x.FunctionId);
        });

        // ─── Permission ───────────────────────────────────────────────────────────
        // Bảng phân quyền: Role X được làm Action Y trên Function Z
        // Composite PK: (RoleId, FunctionId, ActionId) — đủ duy nhất, không cần Id riêng
        builder.Entity<Permission>(e =>
        {
            e.ToTable("Permissions");
            e.HasKey(x => new { x.RoleId, x.FunctionId, x.ActionId });
            e.Property(x => x.FunctionId).HasMaxLength(50);
            e.Property(x => x.ActionId).HasMaxLength(50);

            e.HasOne(x => x.Function)
             .WithMany(f => f.Permissions)
             .HasForeignKey(x => x.FunctionId);

            e.HasOne(x => x.Action)
             .WithMany(a => a.Permissions)
             .HasForeignKey(x => x.ActionId);
        });
    }
}
