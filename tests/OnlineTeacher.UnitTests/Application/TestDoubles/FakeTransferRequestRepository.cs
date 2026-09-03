using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeTransferRequestRepository : ITransferRequestRepository
{
    private readonly List<TransferRequest> _requests = [];

    public IReadOnlyList<TransferRequest> Requests => _requests;

    public void Seed(TransferRequest request)
    {
        _requests.Add(request);
    }

    public Task<TransferRequest?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_requests.FirstOrDefault(r => r.Id == id && r.TenantId == tenantId));

    public Task<IReadOnlyList<TransferRequestResponse>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TransferRequestResponse> result = _requests
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAtUtc)
            .Select(r => new TransferRequestResponse(
                r.Id,
                r.StudentId,
                "Student",
                r.Amount,
                r.PaymentMethod.ToString(),
                r.TransferReference,
                r.Status.ToString(),
                r.CreatedAtUtc))
            .ToList();
        return Task.FromResult(result);
    }

    public void Add(TransferRequest request)
    {
        _requests.Add(request);
    }
}
