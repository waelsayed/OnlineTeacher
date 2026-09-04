using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeStudentCouponRepository : IStudentCouponRepository
{
    private readonly List<StudentCoupon> _coupons = [];

    public IReadOnlyList<StudentCoupon> Coupons => _coupons;

    public void Seed(StudentCoupon coupon)
    {
        _coupons.Add(coupon);
    }

    public Task<StudentCoupon?> GetByCodeAsync(
        Guid tenantId,
        string code,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_coupons.FirstOrDefault(c => c.TenantId == tenantId && c.Code == code));

    public Task<StudentCoupon?> GetByCodeForUpdateAsync(
        Guid tenantId,
        string code,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_coupons.FirstOrDefault(c => c.TenantId == tenantId && c.Code == code));

    public Task<List<StudentCoupon>> ListByTenantAsync(
        Guid tenantId,
        CouponStatus? status,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_coupons
            .Where(c => c.TenantId == tenantId && (status is null || c.Status == status))
            .ToList());

    public Task<StudentCoupon?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_coupons.FirstOrDefault(c => c.Id == id));

    public void Add(StudentCoupon coupon)
    {
        _coupons.Add(coupon);
    }
}
