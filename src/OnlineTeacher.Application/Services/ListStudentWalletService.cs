using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Reads a central Student's wallet and ledger within a single Teacher Platform. The target platform
/// is addressed by publicId; the tenant context is scoped for the tenant-scoped wallet read and
/// restored afterwards. A student can only ever see their own wallet.
/// </summary>
public sealed class ListStudentWalletService
{
    private readonly IPlatformRepository _platforms;
    private readonly IStudentRepository _students;
    private readonly IStudentWalletRepository _wallets;
    private readonly IFinancialTransactionRepository _transactions;
    private readonly ITenantContext _tenantContext;

    public ListStudentWalletService(
        IPlatformRepository platforms,
        IStudentRepository students,
        IStudentWalletRepository wallets,
        IFinancialTransactionRepository transactions,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _students = students;
        _wallets = wallets;
        _transactions = transactions;
        _tenantContext = tenantContext;
    }

    public async Task<WalletDetail?> GetAsync(
        Guid studentId,
        string? teacherPublicId,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);

        var currentTenant = _tenantContext.TenantId;

        try
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.TrySetTenant(platform.Id);
            }

            var wallet = await _wallets.GetByStudentAndTenantAsync(studentId, platform.Id, cancellationToken);

            if (wallet is null)
            {
                return null;
            }

            var transactions = await _transactions.ListByWalletAsync(wallet.Id, cancellationToken);

            return new WalletDetail(wallet.Id, wallet.Balance, "EGP", transactions);
        }
        finally
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.Clear();
            }
        }
    }
}
