using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for the tenant-scoped student wallet. Duplicate (student, tenant) wallets
/// are rejected by a database unique constraint.
/// </summary>
public sealed class StudentWalletRepository : IStudentWalletRepository
{
    private readonly ApplicationDbContext _db;

    public StudentWalletRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<StudentWallet?> GetByStudentAndTenantAsync(
        Guid studentId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        _db.StudentWallets.FirstOrDefaultAsync(
            w => w.StudentId == studentId && w.TenantId == tenantId,
            cancellationToken);

    public void Add(StudentWallet wallet)
    {
        _db.StudentWallets.Add(wallet);
    }
}
