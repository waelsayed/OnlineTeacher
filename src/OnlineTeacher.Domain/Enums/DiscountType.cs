namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// The type of discount a coupon provides. Percentage is calculated relative to the content price;
/// Fixed is a specific monetary amount deducted from the price.
/// </summary>
public enum DiscountType
{
    /// <summary>A percentage discount (1–100%) applied to the content price.</summary>
    Percentage = 0,

    /// <summary>A fixed monetary discount deducted from the content price.</summary>
    Fixed = 1
}