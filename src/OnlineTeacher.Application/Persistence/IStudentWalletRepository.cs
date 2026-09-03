using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for a student's tenant-scoped wallet. A wallet is created lazily when a student
/// first requires one within a platform, and duplicate (student, tenant) wallets are prevented by
/// a database unique constraint.
/// </summary>
public interface IStudentWalletRepository
{
    Task<StudentWallet?> GetByStudentAndTenantAsync(Guid studentId, Guid tenantId, CancellationToken cancellationToken = default);

    void Add(StudentWallet wallet);
}
