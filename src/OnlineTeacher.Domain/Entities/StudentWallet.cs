using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A student's wallet within a single Teacher Platform (tenant). The wallet is tenant-scoped and
/// is owned by the Teacher Platform, not the Central Platform. It is created lazily the first time
/// a student requires one. The balance is maintained in EGP and is reconciled against the financial
/// ledger; the wallet guard prevents the balance from ever going negative.
/// </summary>
public sealed class StudentWallet : IAuditable, ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Current wallet balance in EGP. Never negative.</summary>
    public decimal Balance { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private StudentWallet()
    {
    }

    public StudentWallet(Guid studentId, Guid tenantId)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        TenantId = tenantId;
        Balance = 0m;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Increases the wallet balance by a positive amount and stamps the audit timestamp.</summary>
    public void Credit(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException("Credit amount must be positive.");
        }

        Balance += amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Decreases the wallet balance by a positive amount. Guards against a negative balance so a
    /// wallet can never be overdrawn.
    /// </summary>
    public void Debit(decimal amount)
    {
        if (amount <= 0m)
        {
            throw new DomainException("Debit amount must be positive.");
        }

        if (amount > Balance)
        {
            throw new DomainException("Insufficient wallet balance.");
        }

        Balance -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
