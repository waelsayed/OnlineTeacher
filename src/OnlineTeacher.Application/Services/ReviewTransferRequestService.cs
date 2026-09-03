using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Reviews (approves or rejects) a Pending wallet credit Transfer Request on behalf of the Teacher
/// Platform. The acting teacher must be a member of the resolved tenant; the Wallet.Manage permission
/// is enforced by the API's permission policy. Approving a Pending request credits the student's
/// wallet and records a Credit FinancialTransaction. Rejecting a Pending request marks it Rejected
/// without any credit. Approving/rejecting an already-final request is rejected (idempotency guard),
/// so a wallet can never be credited twice.
/// </summary>
public sealed class ReviewTransferRequestService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IStudentWalletRepository _wallets;
    private readonly IFinancialTransactionRepository _transactions;
    private readonly ITransferRequestRepository _transferRequests;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewTransferRequestService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IStudentWalletRepository wallets,
        IFinancialTransactionRepository transactions,
        ITransferRequestRepository transferRequests,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _wallets = wallets;
        _transactions = transactions;
        _transferRequests = transferRequests;
        _unitOfWork = unitOfWork;
    }

    public async Task ApproveAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid transferRequestId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var request = await _transferRequests.GetByIdAsync(transferRequestId, platform.Id, cancellationToken)
            ?? throw new NotFoundException("Transfer request does not exist.");

        var wallet = await GetWalletForRequestAsync(request, cancellationToken);

        try
        {
            request.Approve();
            wallet.Credit(request.Amount);
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        var transaction = new FinancialTransaction(
            platform.Id,
            wallet.Id,
            request.StudentId,
            TransactionType.Credit,
            request.Amount,
            wallet.Balance - request.Amount,
            wallet.Balance,
            request.Id.ToString(),
            actorTeacherId,
            "teacher");

        _transactions.Add(transaction);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task RejectAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid transferRequestId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var request = await _transferRequests.GetByIdAsync(transferRequestId, platform.Id, cancellationToken)
            ?? throw new NotFoundException("Transfer request does not exist.");

        try
        {
            request.Reject();
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<StudentWallet> GetWalletForRequestAsync(
        TransferRequest request,
        CancellationToken cancellationToken)
    {
        return await _wallets.GetByStudentAndTenantAsync(request.StudentId, request.TenantId, cancellationToken)
            ?? throw new NotFoundException("Wallet does not exist.");
    }
}
