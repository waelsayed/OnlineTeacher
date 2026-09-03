using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeStudentWalletRepository : IStudentWalletRepository
{
    private readonly List<StudentWallet> _wallets = [];

    public IReadOnlyList<StudentWallet> Wallets => _wallets;

    public void Seed(StudentWallet wallet)
    {
        _wallets.Add(wallet);
    }

    public Task<StudentWallet?> GetByStudentAndTenantAsync(
        Guid studentId,
        Guid tenantId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(_wallets.FirstOrDefault(w => w.StudentId == studentId && w.TenantId == tenantId));

    public void Add(StudentWallet wallet)
    {
        _wallets.Add(wallet);
    }
}
