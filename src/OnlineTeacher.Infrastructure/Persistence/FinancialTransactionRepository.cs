using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for the wallet financial ledger. Transactions are immutable historical
/// records and are never deleted.
/// </summary>
public sealed class FinancialTransactionRepository : IFinancialTransactionRepository
{
    private readonly ApplicationDbContext _db;

    public FinancialTransactionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WalletTransactionResponse>> ListByWalletAsync(
        Guid walletId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.FinancialTransactions
            .Where(t => t.WalletId == walletId)
            .OrderByDescending(t => t.OccurredAtUtc)
            .Select(t => new
            {
                t.Id,
                t.Type,
                t.Status,
                t.Amount,
                t.BalanceBefore,
                t.BalanceAfter,
                t.Reference,
                t.OccurredAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows
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
    }

    public void Add(FinancialTransaction transaction)
    {
        _db.FinancialTransactions.Add(transaction);
    }
}
