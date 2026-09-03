using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// A wallet credit Transfer Request as presented to Teacher Platform staff for review.
/// </summary>
public sealed record WalletTransferRequestResponse(
    Guid RequestId,
    Guid StudentId,
    string StudentName,
    decimal Amount,
    string PaymentMethod,
    string? TransferReference,
    string Status,
    DateTime CreatedAtUtc)
{
    public static WalletTransferRequestResponse From(TransferRequestResponse item) =>
        new(
            item.RequestId,
            item.StudentId,
            item.StudentName,
            item.Amount,
            item.PaymentMethod,
            item.TransferReference,
            item.Status,
            item.CreatedAtUtc);
}
