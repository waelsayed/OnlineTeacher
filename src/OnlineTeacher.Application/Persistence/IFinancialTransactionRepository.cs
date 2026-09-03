using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for a wallet's financial ledger. Financial transactions are immutable historical
/// records and are never deleted.
/// </summary>
public interface IFinancialTransactionRepository
{
    Task<IReadOnlyList<WalletTransactionResponse>> ListByWalletAsync(Guid walletId, CancellationToken cancellationToken = default);

    void Add(FinancialTransaction transaction);
}

/// <summary>Projection of a wallet ledger entry for display.</summary>
public sealed record WalletTransactionResponse(
    Guid TransactionId,
    string Type,
    string Status,
    decimal Amount,
    decimal BalanceBefore,
    decimal BalanceAfter,
    string? Reference,
    DateTime OccurredAtUtc);
