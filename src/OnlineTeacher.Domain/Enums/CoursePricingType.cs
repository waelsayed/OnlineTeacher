namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// Explicit commercial state of a Course. A Free course does not require a wallet purchase, while a
/// Paid course requires a positive price and a wallet purchase. The state is explicit and is never
/// inferred from whether a price is present.
/// </summary>
public enum CoursePricingType
{
    /// <summary>The course does not require a wallet purchase.</summary>
    Free = 0,

    /// <summary>The course requires a positive price and a wallet purchase.</summary>
    Paid = 1
}
