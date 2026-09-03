using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A credit Transfer Request submitted by a student to fund their wallet within a Teacher Platform.
/// The request is reviewed by authorized Teacher Platform staff and is either Approved (which leads
/// to a credit FinancialTransaction) or Rejected (no credit). Transfer proof/reference is kept as
/// metadata only; no document/file-management subsystem is introduced.
/// </summary>
public sealed class TransferRequest : IAuditable, ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid WalletId { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid TenantId { get; private set; }

    public decimal Amount { get; private set; }

    public PaymentMethod PaymentMethod { get; private set; }

    /// <summary>Optional external transfer reference supplied by the student. Metadata only.</summary>
    public string? TransferReference { get; private set; }

    public TransferRequestStatus Status { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private TransferRequest()
    {
    }

    public TransferRequest(Guid walletId, Guid studentId, Guid tenantId, decimal amount, PaymentMethod paymentMethod, string? transferReference = null)
    {
        if (walletId == Guid.Empty)
        {
            throw new DomainException("Wallet id is required.");
        }

        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        if (amount <= 0m)
        {
            throw new DomainException("Transfer amount must be positive.");
        }

        Id = Guid.NewGuid();
        WalletId = walletId;
        StudentId = studentId;
        TenantId = tenantId;
        Amount = amount;
        PaymentMethod = paymentMethod;
        TransferReference = string.IsNullOrWhiteSpace(transferReference) ? null : transferReference.Trim();
        Status = TransferRequestStatus.Pending;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Approves a pending request. Only a pending request may be approved (idempotency guard).</summary>
    public void Approve()
    {
        if (Status != TransferRequestStatus.Pending)
        {
            throw new DomainException($"Only a pending transfer request can be approved. Current status is '{Status}'.");
        }

        Status = TransferRequestStatus.Approved;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Rejects a pending request without crediting the wallet. Only a pending request may be rejected.</summary>
    public void Reject()
    {
        if (Status != TransferRequestStatus.Pending)
        {
            throw new DomainException($"Only a pending transfer request can be rejected. Current status is '{Status}'.");
        }

        Status = TransferRequestStatus.Rejected;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
