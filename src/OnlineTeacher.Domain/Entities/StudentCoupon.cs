using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A single-use discount coupon assigned to a specific student within a Teacher Platform (tenant).
/// Coupons are created by teachers and can offer a percentage or fixed discount on Paid Course
/// purchases. A coupon is personal, non-transferable, expirable, and permanently terminal after
/// consumption. The discount is informational/audit-only and does not credit the student wallet.
/// </summary>
public sealed class StudentCoupon : IAuditable, ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>The teacher-defined coupon code, unique within the tenant.</summary>
    public string Code { get; private set; } = string.Empty;

    public DiscountType DiscountType { get; private set; }

    /// <summary>
    /// The discount value. For Percentage discounts, this is a value between 1 and 100 (inclusive).
    /// For Fixed discounts, this is a positive monetary amount in EGP.
    /// </summary>
    public decimal DiscountValue { get; private set; }

    /// <summary>The date and time after which the coupon can no longer be used.</summary>
    public DateTime ExpiresAt { get; private set; }

    public CouponStatus Status { get; private set; }

    /// <summary>The specific Course this coupon is valid for, within the same Teacher Platform (tenant).</summary>
    public Guid CourseId { get; private set; }

    /// <summary>The central Student this coupon is assigned to.</summary>
    public Guid AssignedToStudentId { get; private set; }

    /// <summary>The date and time when the coupon was consumed, or null if not yet consumed.</summary>
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>
    /// The FinancialTransaction id that recorded the consumption. For paid purchases this references
    /// the Purchase transaction; for 100% discount purchases it references the CouponCredit transaction.
    /// </summary>
    public Guid? ConsumedInTransactionId { get; private set; }

    /// <summary>The Teacher (or Assistant) who created this coupon.</summary>
    public Guid CreatedByTeacherId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private StudentCoupon()
    {
    }

    public StudentCoupon(
        Guid tenantId,
        string code,
        DiscountType discountType,
        decimal discountValue,
        DateTime expiresAt,
        Guid courseId,
        Guid assignedToStudentId,
        Guid createdByTeacherId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("Coupon code is required.");
        }

        var normalizedCode = code.Trim();

        if (normalizedCode.Length == 0)
        {
            throw new DomainException("Coupon code is required.");
        }

        if (discountType == DiscountType.Percentage && (discountValue < 1m || discountValue > 100m))
        {
            throw new DomainException("Percentage discount must be between 1 and 100.");
        }

        if (discountType == DiscountType.Fixed && discountValue <= 0m)
        {
            throw new DomainException("Fixed discount must be positive.");
        }

        if (expiresAt <= DateTime.UtcNow)
        {
            throw new DomainException("Expiration date must be in the future.");
        }

        if (assignedToStudentId == Guid.Empty)
        {
            throw new DomainException("Assigned student id is required.");
        }

        if (courseId == Guid.Empty)
        {
            throw new DomainException("Course id is required.");
        }

        if (createdByTeacherId == Guid.Empty)
        {
            throw new DomainException("Created by teacher id is required.");
        }

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        DiscountType = discountType;
        DiscountValue = discountValue;
        ExpiresAt = expiresAt;
        Status = CouponStatus.Active;
        CourseId = courseId;
        AssignedToStudentId = assignedToStudentId;
        CreatedByTeacherId = createdByTeacherId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Consumes this coupon for the specified student within the given transaction.
    /// Validates that the coupon is active, not expired, belongs to the specified student,
    /// and is applicable to the specified Course.
    /// </summary>
    public void Consume(Guid studentId, Guid courseId, Guid transactionId)
    {
        if (Status != CouponStatus.Active)
        {
            throw new DomainException("Coupon is not active.");
        }

        if (DateTime.UtcNow > ExpiresAt)
        {
            Status = CouponStatus.Expired;
            throw new DomainException("Coupon has expired.");
        }

        if (AssignedToStudentId != studentId)
        {
            throw new DomainException("This coupon is assigned to a different student.");
        }

        if (CourseId != courseId)
        {
            throw new DomainException("This coupon is not valid for the specified course.");
        }

        if (transactionId == Guid.Empty)
        {
            throw new DomainException("Transaction id is required.");
        }

        Status = CouponStatus.Consumed;
        ConsumedAt = DateTime.UtcNow;
        ConsumedInTransactionId = transactionId;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes an active coupon, rendering it expired. A consumed coupon cannot be revoked.
    /// </summary>
    public void Revoke()
    {
        if (Status != CouponStatus.Active)
        {
            throw new DomainException("Only active coupons can be revoked.");
        }

        Status = CouponStatus.Expired;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the discount amount that should be applied to the given content price.
    /// The discount is capped at the price so the final amount never goes below zero.
    /// </summary>
    public decimal CalculateDiscount(decimal price)
    {
        if (price < 0m)
        {
            throw new DomainException("Price cannot be negative.");
        }

        var discount = DiscountType switch
        {
            DiscountType.Percentage => price * DiscountValue / 100m,
            DiscountType.Fixed => DiscountValue,
            _ => throw new DomainException("Unknown discount type.")
        };

        return Math.Min(discount, price);
    }

    /// <summary>
    /// Calculates the final amount payable after applying the coupon discount.
    /// Returns zero if the discount equals or exceeds the price.
    /// </summary>
    public decimal GetFinalAmount(decimal price)
    {
        return Math.Max(0m, price - CalculateDiscount(price));
    }

    /// <summary>
    /// Returns true when the coupon would reduce the price to zero (100% discount case).
    /// </summary>
    public bool IsFullDiscount(decimal price)
    {
        return GetFinalAmount(price) == 0m;
    }
}