using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Records a student's request to credit their wallet within a Teacher Platform via an external
/// transfer (e.g. Vodafone Cash or InstaPay). The target platform is addressed by publicId and the
/// tenant context is scoped for the tenant-scoped wallet/transfer reads then restored. The wallet
/// is created lazily the first time a student requires one in a platform. The request is created in
/// a Pending state and awaits review by authorized Teacher Platform staff.
/// </summary>
public sealed class SubmitTransferRequestService
{
    private readonly IPlatformRepository _platforms;
    private readonly IStudentRepository _students;
    private readonly IStudentWalletRepository _wallets;
    private readonly ITransferRequestRepository _transferRequests;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public SubmitTransferRequestService(
        IPlatformRepository platforms,
        IStudentRepository students,
        IStudentWalletRepository wallets,
        ITransferRequestRepository transferRequests,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _students = students;
        _wallets = wallets;
        _transferRequests = transferRequests;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> SubmitAsync(
        Guid studentId,
        string? teacherPublicId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? transferReference,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);

        if (platform.Status != PlatformStatus.Active)
        {
            throw new BusinessRuleViolationException("The teacher platform is not active.");
        }

        if (amount <= 0m)
        {
            throw new ValidationException("Transfer amount must be positive.");
        }

        var currentTenant = _tenantContext.TenantId;

        try
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.TrySetTenant(platform.Id);
            }

            var wallet = await _wallets.GetByStudentAndTenantAsync(studentId, platform.Id, cancellationToken)
                ?? CreateWallet(studentId, platform.Id);

            var request = new TransferRequest(
                wallet.Id,
                studentId,
                platform.Id,
                amount,
                paymentMethod,
                transferReference);

            _transferRequests.Add(request);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return request.Id;
        }
        finally
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.Clear();
            }
        }
    }

    private StudentWallet CreateWallet(Guid studentId, Guid tenantId)
    {
        var wallet = new StudentWallet(studentId, tenantId);
        _wallets.Add(wallet);
        return wallet;
    }
}
