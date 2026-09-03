using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for a tenant's wallet credit transfer requests. Requests are reviewed by authorized
/// staff and are terminal once approved or rejected.
/// </summary>
public interface ITransferRequestRepository
{
    Task<TransferRequest?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TransferRequestResponse>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    void Add(TransferRequest request);
}

/// <summary>Projection of a transfer request for teacher-side review.</summary>
public sealed record TransferRequestResponse(
    Guid RequestId,
    Guid StudentId,
    string StudentName,
    decimal Amount,
    string PaymentMethod,
    string? TransferReference,
    string Status,
    DateTime CreatedAtUtc);
