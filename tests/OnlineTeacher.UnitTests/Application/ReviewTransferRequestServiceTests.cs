using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ReviewTransferRequestServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakeStudentWalletRepository _wallets = new();
    private readonly FakeFinancialTransactionRepository _transactions = new();
    private readonly FakeTransferRequestRepository _transferRequests = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ReviewTransferRequestService CreateService() =>
        new(_platforms, _access, _wallets, _transactions, _transferRequests, _unitOfWork);

    private (TeacherPlatform Platform, Guid TeacherId) SeedMember(string publicId = "AbCdEf123456")
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        _platforms.Seed(platform);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, new TeacherPlatformAccess(
            teacherId,
            platform.Id,
            platform.PublicId.Value,
            platform.Slug.Value,
            PlatformStatus.Active,
            true,
            ["Owner"],
            ["Wallet.Manage"]));
        return (platform, teacherId);
    }

    private (StudentWallet Wallet, TransferRequest Request) SeedPendingRequest(
        TeacherPlatform platform,
        Guid studentId,
        decimal amount = 200m)
    {
        var wallet = new StudentWallet(studentId, platform.Id);
        _wallets.Seed(wallet);
        var request = new TransferRequest(wallet.Id, studentId, platform.Id, amount, PaymentMethod.VodafoneCash, "REF");
        _transferRequests.Seed(request);
        return (wallet, request);
    }

    [Fact]
    public async Task Approve_PendingRequest_CreditsWalletAndRecordsCreditTransaction()
    {
        var (platform, teacherId) = SeedMember();
        var (wallet, request) = SeedPendingRequest(platform, Guid.NewGuid(), 200m);

        await CreateService().ApproveAsync(teacherId, platform.PublicId.Value, request.Id);

        request.Status.Should().Be(TransferRequestStatus.Approved);
        wallet.Balance.Should().Be(200m);

        var transaction = _transactions.Transactions.Should().ContainSingle().Subject;
        transaction.Type.Should().Be(TransactionType.Credit);
        transaction.Amount.Should().Be(200m);
        transaction.BalanceBefore.Should().Be(0m);
        transaction.BalanceAfter.Should().Be(200m);
        transaction.Reference.Should().Be(request.Id.ToString());
        transaction.ActorType.Should().Be("teacher");
        transaction.ActorId.Should().Be(teacherId);

        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Reject_PendingRequest_MarksRejectedWithoutCredit()
    {
        var (platform, teacherId) = SeedMember();
        var (wallet, request) = SeedPendingRequest(platform, Guid.NewGuid(), 200m);

        await CreateService().RejectAsync(teacherId, platform.PublicId.Value, request.Id);

        request.Status.Should().Be(TransferRequestStatus.Rejected);
        wallet.Balance.Should().Be(0m);
        _transactions.Transactions.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Approve_AlreadyApprovedRequest_ThrowsBusinessRuleViolationAndNoDoubleCredit()
    {
        var (platform, teacherId) = SeedMember();
        var (wallet, request) = SeedPendingRequest(platform, Guid.NewGuid(), 200m);
        await CreateService().ApproveAsync(teacherId, platform.PublicId.Value, request.Id);
        var balanceAfterFirst = wallet.Balance;

        var act = () => CreateService().ApproveAsync(teacherId, platform.PublicId.Value, request.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*pending*approved*");

        wallet.Balance.Should().Be(balanceAfterFirst);
        _transactions.Transactions.Should().ContainSingle();
    }

    [Fact]
    public async Task Reject_AlreadyApprovedRequest_ThrowsBusinessRuleViolation()
    {
        var (platform, teacherId) = SeedMember();
        var (wallet, request) = SeedPendingRequest(platform, Guid.NewGuid(), 200m);
        await CreateService().ApproveAsync(teacherId, platform.PublicId.Value, request.Id);

        var act = () => CreateService().RejectAsync(teacherId, platform.PublicId.Value, request.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*pending*rejected*");
    }

    [Fact]
    public async Task Approve_UnknownRequest_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();

        var act = () => CreateService().ApproveAsync(teacherId, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Approve_NonMemberActor_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();
        var (_, request) = SeedPendingRequest(platform, Guid.NewGuid());

        var act = () => CreateService().ApproveAsync(Guid.NewGuid(), platform.PublicId.Value, request.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task Approve_InvalidPublicId_ThrowsValidation()
    {
        var (_, teacherId) = SeedMember();
        var (_, request) = SeedPendingRequest(_platforms.Platforms[0], Guid.NewGuid());

        var act = () => CreateService().ApproveAsync(teacherId, "not-a-public-id", request.Id);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Approve_UnknownPlatform_ThrowsNotFound()
    {
        var (_, teacherId) = SeedMember();
        var (_, request) = SeedPendingRequest(_platforms.Platforms[0], Guid.NewGuid());

        var act = () => CreateService().ApproveAsync(teacherId, PublicId.Generate().Value, request.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
