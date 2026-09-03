using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// A wallet ledger entry as presented to a student. Mapped from the application's wallet
/// transaction projection.
/// </summary>
public sealed record WalletTransactionResponse(
    Guid TransactionId,
    string Type,
    string Status,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Reference,
    DateTime OccurredAtUtc)
{
    public static WalletTransactionResponse From(OnlineTeacher.Application.Persistence.WalletTransactionResponse item) =>
        new(item.TransactionId, item.Type, item.Status, item.Amount, item.BalanceBefore, item.BalanceAfter, item.Reference, item.OccurredAtUtc);
}

/// <summary>
/// A student's wallet within a Teacher Platform, with balance and ledger history, as presented
/// to the student.
/// </summary>
public sealed record WalletDetailResponse(
    Guid WalletId,
    decimal Balance,
    string Currency,
    IReadOnlyList<WalletTransactionResponse> Transactions)
{
    public static WalletDetailResponse From(WalletDetail detail) =>
        new(
            detail.WalletId,
            detail.Balance,
            detail.Currency,
            detail.Transactions.Select(WalletTransactionResponse.From).ToList());
}
