using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for a student coupon within a Teacher Platform (tenant).
/// </summary>
public interface IStudentCouponRepository
{
    Task<StudentCoupon?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);

    Task<StudentCoupon?> GetByCodeForUpdateAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);

    Task<List<StudentCoupon>> ListByTenantAsync(Guid tenantId, CouponStatus? status, CancellationToken cancellationToken = default);

    Task<StudentCoupon?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    void Add(StudentCoupon coupon);
}