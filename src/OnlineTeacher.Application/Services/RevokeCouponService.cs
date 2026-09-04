using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Revokes (deactivates) an Active Student Coupon within the resolved Teacher Platform. The acting
/// teacher must be a member of the tenant; the <c>Coupon.Manage</c> permission is enforced by the
/// API's permission policy. Only an Active coupon belonging to the resolved tenant can be revoked;
/// an already-consumed or already-expired coupon is rejected.
/// </summary>
public sealed class RevokeCouponService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IStudentCouponRepository _coupons;
    private readonly IUnitOfWork _unitOfWork;

    public RevokeCouponService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IStudentCouponRepository coupons,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _coupons = coupons;
        _unitOfWork = unitOfWork;
    }

    public async Task RevokeAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid couponId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var coupon = await _coupons.GetByIdAsync(couponId, cancellationToken);

        if (coupon is null || coupon.TenantId != platform.Id)
        {
            throw new NotFoundException("Coupon does not exist.");
        }

        try
        {
            coupon.Revoke();
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
