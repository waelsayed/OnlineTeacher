using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeFinancialTransactionRepository : IFinancialTransactionRepository
{
    private readonly List<FinancialTransaction> _transactions = [];

    public IReadOnlyList<FinancialTransaction> Transactions => _transactions;

    public void Seed(FinancialTransaction transaction)
    {
        _transactions.Add(transaction);
    }

    public Task<IReadOnlyList<WalletTransactionResponse>> ListByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<WalletTransactionResponse> result = _transactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.OccurredAtUtc)
            .Select(t => new WalletTransactionResponse(
                t.Id,
                t.Type.ToString(),
                t.Status.ToString(),
                t.Amount,
                t.BalanceBefore,
                t.BalanceAfter,
                t.Reference,
                t.OccurredAtUtc))
            .ToList();
        return Task.FromResult(result);
    }

    public void Add(FinancialTransaction transaction)
    {
        _transactions.Add(transaction);
    }
}
