using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for tenant-scoped student coupons.
/// </summary>
public sealed class StudentCouponRepository : IStudentCouponRepository
{
    private readonly ApplicationDbContext _db;

    public StudentCouponRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<StudentCoupon?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default) =>
        _db.StudentCoupons.FirstOrDefaultAsync(
            c => c.TenantId == tenantId && c.Code == code,
            cancellationToken);

    public async Task<StudentCoupon?> GetByCodeForUpdateAsync(Guid tenantId, string code, CancellationToken cancellationToken = default) =>
        await _db.StudentCoupons
            .FromSqlInterpolated($"SELECT * FROM student_coupons WHERE tenant_id = {tenantId} AND code = {code} FOR UPDATE")
            .AsTracking()
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<List<StudentCoupon>> ListByTenantAsync(Guid tenantId, CouponStatus? status, CancellationToken cancellationToken = default) =>
        await _db.StudentCoupons
            .Where(c => c.TenantId == tenantId)
            .Where(c => !status.HasValue || c.Status == status.Value)
            .OrderBy(c => c.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public Task<StudentCoupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.StudentCoupons.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(StudentCoupon coupon)
    {
        _db.StudentCoupons.Add(coupon);
    }
}