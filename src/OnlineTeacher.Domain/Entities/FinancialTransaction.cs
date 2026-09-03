using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// An immutable, auditable entry in a student wallet's financial ledger. Every change to a wallet
/// balance is recorded as a financial transaction (credit / purchase-debit). Financial transactions
/// are historical records and must never be silently modified or deleted.
/// </summary>
public sealed class FinancialTransaction : ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid WalletId { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid TenantId { get; private set; }

    public TransactionType Type { get; private set; }

    /// <summary>Explicit outcome of the transaction. A ledger entry is only a successful transaction when its status is Completed.</summary>
    public FinancialTransactionStatus Status { get; private set; }

    /// <summary>Signed amount: positive grows the balance (credit), negative reduces it (purchase debit).</summary>
    public decimal Amount { get; private set; }

    public decimal BalanceBefore { get; private set; }

    public decimal BalanceAfter { get; private set; }

    /// <summary>Optional reference to the business source of this transaction (e.g. a course id or transfer request id).</summary>
    public string? Reference { get; private set; }

    public Guid ActorId { get; private set; }

    /// <summary>The principal type that performed the transaction (e.g. "student", "teacher").</summary>
    public string ActorType { get; private set; } = string.Empty;

    public DateTime OccurredAtUtc { get; private set; }

    private FinancialTransaction()
    {
    }

    /// <summary>
    /// Records a completed financial transaction. The signed amount and the before/after balances
    /// allow the ledger to reconstruct the running balance independently of the wallet field.
    /// </summary>
    public FinancialTransaction(
        Guid tenantId,
        Guid walletId,
        Guid studentId,
        TransactionType type,
        decimal amount,
        decimal balanceBefore,
        decimal balanceAfter,
        string? reference,
        Guid actorId,
        string actorType)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        if (walletId == Guid.Empty)
        {
            throw new DomainException("Wallet id is required.");
        }

        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (string.IsNullOrWhiteSpace(actorType))
        {
            throw new DomainException("Actor type is required.");
        }

        Id = Guid.NewGuid();
        TenantId = tenantId;
        WalletId = walletId;
        StudentId = studentId;
        Type = type;
        Status = FinancialTransactionStatus.Completed;
        Amount = amount;
        BalanceBefore = balanceBefore;
        BalanceAfter = balanceAfter;
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        ActorId = actorId;
        ActorType = actorType.Trim();
        OccurredAtUtc = DateTime.UtcNow;
    }
}
