namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// The lifecycle state of a student coupon. Active coupons may be consumed or revoked;
/// Consumed and Expired are terminal states.
/// </summary>
public enum CouponStatus
{
    /// <summary>The coupon has been created and is available for use.</summary>
    Active = 0,

    /// <summary>The coupon has been used by its assigned student and is no longer available.</summary>
    Consumed = 1,

    /// <summary>The coupon has expired or been revoked and is no longer available.</summary>
    Expired = 2
}