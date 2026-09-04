using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Reads a single Student Coupon's detail within the resolved Teacher Platform. The acting teacher
/// must be a member of the tenant; the <c>Coupon.Manage</c> permission is enforced by the API's
/// permission policy. Only coupons belonging to the resolved tenant can be returned.
/// </summary>
public sealed class GetCouponService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IStudentCouponRepository _coupons;
    private readonly IStudentRepository _students;

    public GetCouponService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IStudentCouponRepository coupons,
        IStudentRepository students)
    {
        _platforms = platforms;
        _access = access;
        _coupons = coupons;
        _students = students;
    }

    public async Task<CouponDto> GetAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid couponId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var coupon = await LoadCouponAsync(platform.Id, couponId, cancellationToken)
            ?? throw new NotFoundException("Coupon does not exist.");

        var student = await _students.GetByIdAsync(coupon.AssignedToStudentId, cancellationToken);

        return new CouponDto(
            coupon.Id,
            coupon.Code,
            coupon.DiscountType,
            coupon.DiscountValue,
            coupon.ExpiresAt,
            coupon.Status,
            coupon.CourseId,
            coupon.AssignedToStudentId,
            student?.Name,
            coupon.ConsumedAt,
            coupon.ConsumedInTransactionId,
            coupon.CreatedAtUtc);
    }

    private async Task<StudentCoupon?> LoadCouponAsync(Guid tenantId, Guid couponId, CancellationToken cancellationToken)
    {
        var coupon = await _coupons.GetByIdAsync(couponId, cancellationToken);
        return coupon is not null && coupon.TenantId == tenantId ? coupon : null;
    }
}
