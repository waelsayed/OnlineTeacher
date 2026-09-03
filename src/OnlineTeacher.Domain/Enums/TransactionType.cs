namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// The kind of movement on a wallet's financial ledger. Credit grows the balance (e.g. an
/// approved transfer request); Purchase (a debit) reduces it when a student buys content.
/// The Refund and CouponCredit types are reserved for future work and are not implemented
/// as flows in the current scope.
/// </summary>
public enum TransactionType
{
    /// <summary>The wallet balance is increased (e.g. wallet credit through an approved transfer).</summary>
    Credit = 0,

    /// <summary>The wallet balance is reduced to purchase content (a debit).</summary>
    Purchase = 1,

    /// <summary>Reserved for a future refund flow. Not implemented.</summary>
    Refund = 2,

    /// <summary>Reserved for a future coupon-credit flow. Not implemented.</summary>
    CouponCredit = 3,

    /// <summary>Reserved for a future manual correction flow. Not implemented.</summary>
    Adjustment = 4
}
