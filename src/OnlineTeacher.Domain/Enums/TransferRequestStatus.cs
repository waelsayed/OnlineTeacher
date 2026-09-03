namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// Lifecycle state of a wallet credit Transfer Request. A request is Pending until reviewed by
/// authorized Teacher Platform staff, then it is either Approved (wallet credited) or Rejected
/// (no credit). Both Approved and Rejected are terminal states.
/// </summary>
public enum TransferRequestStatus
{
    /// <summary>The request has been submitted and awaits review.</summary>
    Pending = 0,

    /// <summary>The request was approved; the wallet credit transaction was recorded.</summary>
    Approved = 1,

    /// <summary>The request was rejected; no wallet credit was applied.</summary>
    Rejected = 2
}
