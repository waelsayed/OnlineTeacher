using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// A student's wallet within a Teacher Platform including its current balance and ledger history.
/// </summary>
public sealed record WalletDetail(
    Guid WalletId,
    decimal Balance,
    string Currency,
    IReadOnlyList<WalletTransactionResponse> Transactions);
