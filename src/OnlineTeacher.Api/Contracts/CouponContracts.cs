using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request to create a single-use Student Coupon assigned to a specific student and tied to a
/// specific Course within a Teacher Platform.
/// </summary>
public sealed record CreateCouponRequest(
    string Code,
    string DiscountType,        // "Percentage" | "Fixed"
    decimal DiscountValue,
    DateTime ExpiresAt,
    Guid CourseId,
    Guid StudentId);

/// <summary>
/// Response projection for a Student Coupon as presented to a teacher, mapped from the application's
/// <see cref="CouponDto"/>.
/// </summary>
public sealed record CouponResponse(
    Guid Id,
    string Code,
    string DiscountType,
    decimal DiscountValue,
    DateTime ExpiresAt,
    string Status,
    Guid CourseId,
    Guid StudentId,
    string? StudentName,
    DateTime? ConsumedAt,
    Guid? ConsumedInTransactionId,
    DateTime CreatedAtUtc)
{
    public static CouponResponse From(CouponDto coupon) =>
        new(
            coupon.Id,
            coupon.Code,
            coupon.DiscountType.ToString(),
            coupon.DiscountValue,
            coupon.ExpiresAt,
            coupon.Status.ToString(),
            coupon.CourseId,
            coupon.StudentId,
            coupon.StudentName,
            coupon.ConsumedAt,
            coupon.ConsumedInTransactionId,
            coupon.CreatedAtUtc);
}

/// <summary>
/// Optional body for a student course purchase. Only <c>CouponCode</c> is currently supported.
/// When omitted, the course is purchased at full price (existing behavior).
/// </summary>
public sealed record PurchaseRequest(string? CouponCode);
