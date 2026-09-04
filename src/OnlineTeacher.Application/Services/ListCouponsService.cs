using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Lists the Student Coupons of the resolved Teacher Platform, optionally filtered by status. The
/// acting teacher must be a member of the tenant; the <c>Coupon.Manage</c> permission is enforced by
/// the API's permission policy. Returns coupon details including the assigned student's name.
/// </summary>
public sealed class ListCouponsService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IStudentCouponRepository _coupons;
    private readonly IStudentRepository _students;

    public ListCouponsService(
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

    public async Task<IReadOnlyList<CouponDto>> ListAsync(
        Guid actorTeacherId,
        string? publicId,
        CouponStatus? status,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var coupons = await _coupons.ListByTenantAsync(platform.Id, status, cancellationToken);
        var result = new List<CouponDto>(coupons.Count);

        foreach (var coupon in coupons)
        {
            result.Add(await ToDtoAsync(coupon, cancellationToken));
        }

        return result;
    }

    private async Task<CouponDto> ToDtoAsync(StudentCoupon coupon, CancellationToken cancellationToken)
    {
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
}
