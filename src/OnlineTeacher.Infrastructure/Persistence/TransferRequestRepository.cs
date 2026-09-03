using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for a tenant's wallet credit transfer requests.
/// </summary>
public sealed class TransferRequestRepository : ITransferRequestRepository
{
    private readonly ApplicationDbContext _db;

    public TransferRequestRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<TransferRequest?> GetByIdAsync(
        Guid id,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        _db.TransferRequests.FirstOrDefaultAsync(
            r => r.Id == id && r.TenantId == tenantId,
            cancellationToken);

    public async Task<IReadOnlyList<TransferRequestResponse>> ListByTenantAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.TransferRequests
            .Where(r => r.TenantId == tenantId)
            .Join(_db.Students,
                r => r.StudentId,
                s => s.Id,
                (r, s) => new { Request = r, Student = s })
            .OrderByDescending(x => x.Request.CreatedAtUtc)
            .Select(x => new
            {
                x.Request.Id,
                StudentId = x.Student.Id,
                x.Student.Name,
                x.Request.Amount,
                x.Request.PaymentMethod,
                x.Request.TransferReference,
                x.Request.Status,
                x.Request.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new TransferRequestResponse(
                x.Id,
                x.StudentId,
                x.Name,
                x.Amount,
                x.PaymentMethod.ToString(),
                x.TransferReference,
                x.Status.ToString(),
                x.CreatedAtUtc))
            .ToList();
    }

    public void Add(TransferRequest request)
    {
        _db.TransferRequests.Add(request);
    }
}
