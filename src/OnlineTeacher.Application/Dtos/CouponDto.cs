using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Projection of a Student Coupon for teacher management views, including the assigned student's name.
/// </summary>
public sealed record CouponDto(
    Guid Id,
    string Code,
    DiscountType DiscountType,
    decimal DiscountValue,
    DateTime ExpiresAt,
    CouponStatus Status,
    Guid CourseId,
    Guid StudentId,
    string? StudentName,
    DateTime? ConsumedAt,
    Guid? ConsumedInTransactionId,
    DateTime CreatedAtUtc);
