using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class SubmitTransferRequestServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeStudentWalletRepository _wallets = new();
    private readonly FakeTransferRequestRepository _transferRequests = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private SubmitTransferRequestService CreateService() =>
        new(_platforms, _students, _wallets, _transferRequests, _unitOfWork, _tenantContext);

    private (Student Student, TeacherPlatform Platform) SeedEligibleTarget(string publicId = "AbCdEf123456")
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        platform.Activate();
        _students.Seed(student);
        _platforms.Seed(platform);
        return (student, platform);
    }

    [Fact]
    public async Task Submit_ValidRequest_CreatesPendingTransferRequestAndWallet()
    {
        var (student, platform) = SeedEligibleTarget();

        var id = await CreateService().SubmitAsync(
            student.Id,
            platform.PublicId.Value,
            200m,
            PaymentMethod.VodafoneCash,
            "REF-123");

        var wallet = _wallets.Wallets.Should().ContainSingle().Subject;
        wallet.StudentId.Should().Be(student.Id);
        wallet.TenantId.Should().Be(platform.Id);
        wallet.Balance.Should().Be(0m);

        var request = _transferRequests.Requests.Should().ContainSingle().Subject;
        request.Id.Should().Be(id);
        request.WalletId.Should().Be(wallet.Id);
        request.StudentId.Should().Be(student.Id);
        request.TenantId.Should().Be(platform.Id);
        request.Amount.Should().Be(200m);
        request.PaymentMethod.Should().Be(PaymentMethod.VodafoneCash);
        request.TransferReference.Should().Be("REF-123");
        request.Status.Should().Be(TransferRequestStatus.Pending);

        _unitOfWork.SaveCount.Should().Be(1);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Submit_ExistingWallet_ReusesWallet()
    {
        var (student, platform) = SeedEligibleTarget();
        var existing = new StudentWallet(student.Id, platform.Id);
        existing.Credit(50m);
        _wallets.Seed(existing);

        await CreateService().SubmitAsync(
            student.Id,
            platform.PublicId.Value,
            100m,
            PaymentMethod.InstaPay,
            null);

        _wallets.Wallets.Should().ContainSingle();
        _wallets.Wallets[0].Balance.Should().Be(50m);
        _transferRequests.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Submit_ZeroAmount_ThrowsValidation()
    {
        var (student, platform) = SeedEligibleTarget();

        var act = () => CreateService().SubmitAsync(student.Id, platform.PublicId.Value, 0m, PaymentMethod.InstaPay, null);

        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*positive*");
    }

    [Fact]
    public async Task Submit_InactivePlatform_ThrowsBusinessRuleViolation()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create("AbCdEf123456"), Slug.CreateFromName("my-platform"));
        _students.Seed(student);
        _platforms.Seed(platform);

        var act = () => CreateService().SubmitAsync(student.Id, platform.PublicId.Value, 100m, PaymentMethod.InstaPay, null);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Submit_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform) = SeedEligibleTarget();

        var act = () => CreateService().SubmitAsync(Guid.NewGuid(), platform.PublicId.Value, 100m, PaymentMethod.InstaPay, null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Submit_InvalidPublicId_ThrowsValidation()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().SubmitAsync(student.Id, "not-a-public-id", 100m, PaymentMethod.InstaPay, null);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Submit_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().SubmitAsync(student.Id, PublicId.Generate().Value, 100m, PaymentMethod.InstaPay, null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Submit_UnderTenantContext_ThrowsTenantMismatch()
    {
        var (student, platform) = SeedEligibleTarget();
        _tenantContext.TrySetTenant(platform.Id);

        var act = () => CreateService().SubmitAsync(student.Id, platform.PublicId.Value, 100m, PaymentMethod.InstaPay, null);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}
