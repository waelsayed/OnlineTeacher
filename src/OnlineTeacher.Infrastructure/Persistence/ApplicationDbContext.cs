using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Application data context. Applies tenant query filters as a data-layer defense:
/// this guards against accidental cross-tenant access but is not a replacement for authorization.
/// </summary>
public sealed class ApplicationDbContext : DbContext
{
    private readonly ITenantContext _tenantContext;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>Current tenant for the scope; null when no tenant context is active.</summary>
    public Guid? CurrentTenantId => _tenantContext.TenantId;

    public DbSet<Teacher> Teachers => Set<Teacher>();

    public DbSet<TeacherPlatform> TeacherPlatforms => Set<TeacherPlatform>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<Permission> Permissions => Set<Permission>();

    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();

    public DbSet<TeacherPlatformMembership> Memberships => Set<TeacherPlatformMembership>();

    public DbSet<Student> Students => Set<Student>();

    public DbSet<StudentFollow> StudentFollows => Set<StudentFollow>();

    public DbSet<Course> Courses => Set<Course>();

    public DbSet<Unit> Units => Set<Unit>();

    public DbSet<Lesson> Lessons => Set<Lesson>();

    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public DbSet<StudentWallet> StudentWallets => Set<StudentWallet>();

    public DbSet<FinancialTransaction> FinancialTransactions => Set<FinancialTransaction>();

    public DbSet<TransferRequest> TransferRequests => Set<TransferRequest>();

    public DbSet<StudentCoupon> StudentCoupons => Set<StudentCoupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<Role>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<RolePermission>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<TeacherPlatformMembership>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Course>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Unit>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Lesson>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<Enrollment>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<StudentWallet>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<FinancialTransaction>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<TransferRequest>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
        modelBuilder.Entity<StudentCoupon>().HasQueryFilter(e => e.TenantId == CurrentTenantId);
    }
}