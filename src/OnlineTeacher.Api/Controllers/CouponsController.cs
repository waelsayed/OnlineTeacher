using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Teacher Platform Student Coupon management. The route-template parameters {publicId} and {slug}
/// are validated by TenantRouteMiddleware, which resolves and scopes the tenant. All coupon
/// management endpoints require the <c>Coupon.Manage</c> permission. The application services further
/// enforce that the acting user is a member of the resolved tenant, so a valid cross-tenant JWT
/// cannot manage another platform's coupons.
/// </summary>
[ApiController]
[Route("{publicId}/{slug}/api/platform/coupons")]
[Authorize]
public sealed class CouponsController : ControllerBase
{
    private readonly CreateCouponService _create;
    private readonly ListCouponsService _list;
    private readonly GetCouponService _get;
    private readonly RevokeCouponService _revoke;

    public CouponsController(
        CreateCouponService create,
        ListCouponsService list,
        GetCouponService get,
        RevokeCouponService revoke)
    {
        _create = create;
        _list = list;
        _get = get;
        _revoke = revoke;
    }

    [HttpPost]
    [RequirePermission("Coupon.Manage")]
    public async Task<IActionResult> Create(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromBody] CreateCouponRequest request,
        CancellationToken cancellationToken)
    {
        var couponId = await _create.CreateAsync(
            GetTeacherIdClaim(),
            publicId,
            request.Code,
            ParseDiscountType(request.DiscountType),
            request.DiscountValue,
            request.ExpiresAt,
            request.CourseId,
            request.StudentId,
            cancellationToken);

        return Created(string.Empty, new { couponId });
    }

    [HttpGet]
    [RequirePermission("Coupon.Manage")]
    public async Task<IActionResult> List(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var coupons = await _list.ListAsync(
            GetTeacherIdClaim(),
            publicId,
            ParseOptionalStatus(status),
            cancellationToken);

        return Ok(coupons.Select(CouponResponse.From));
    }

    [HttpGet("{couponId:guid}")]
    [RequirePermission("Coupon.Manage")]
    public async Task<IActionResult> Get(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid couponId,
        CancellationToken cancellationToken)
    {
        var coupon = await _get.GetAsync(GetTeacherIdClaim(), publicId, couponId, cancellationToken);
        return Ok(CouponResponse.From(coupon));
    }

    [HttpDelete("{couponId:guid}")]
    [RequirePermission("Coupon.Manage")]
    public async Task<IActionResult> Revoke(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid couponId,
        CancellationToken cancellationToken)
    {
        await _revoke.RevokeAsync(GetTeacherIdClaim(), publicId, couponId, cancellationToken);
        return NoContent();
    }

    private static DiscountType ParseDiscountType(string? discountType)
    {
        if (string.IsNullOrWhiteSpace(discountType))
        {
            throw new OnlineTeacher.Application.Exceptions.ValidationException("Discount type is required.");
        }

        if (!Enum.TryParse<DiscountType>(discountType, ignoreCase: true, out var parsed))
        {
            throw new OnlineTeacher.Application.Exceptions.ValidationException($"Unknown discount type '{discountType}'.");
        }

        return parsed;
    }

    private static CouponStatus? ParseOptionalStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (!Enum.TryParse<CouponStatus>(status, ignoreCase: true, out var parsed))
        {
            throw new OnlineTeacher.Application.Exceptions.ValidationException($"Unknown coupon status '{status}'.");
        }

        return parsed;
    }

    private Guid GetTeacherIdClaim()
    {
        var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        var raw = sub?.Value;
        if (raw is null || !Guid.TryParse(raw, out var teacherId))
        {
            throw new UnauthorizedAccessException("The token does not carry a valid teacher identity.");
        }

        return teacherId;
    }
}
