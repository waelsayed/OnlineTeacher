using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ActivateTeacherPlatformServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private ActivateTeacherPlatformService CreateService() =>
        new(_platforms, _unitOfWork, _tenantContext);

    private TeacherPlatform SeedPendingPlatform()
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Generate(), Slug.CreateFromName("My Platform"));
        _platforms.Seed(platform);
        return platform;
    }

    [Fact]
    public async Task Activate_PendingPlatform_TransitionsToActiveAndRecordsTimestamp()
    {
        var platform = SeedPendingPlatform();
        var service = CreateService();

        var result = await service.ActivateAsync(platform.PublicId.Value);

        platform.Status.Should().Be(PlatformStatus.Active);
        platform.ActivatedAtUtc.Should().NotBeNull();
        result.PlatformId.Should().Be(platform.Id);
        result.PublicId.Should().Be(platform.PublicId.Value);
        result.ActivatedAtUtc.Should().Be(platform.ActivatedAtUtc!.Value);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Activate_AlreadyActivePlatform_ThrowsBusinessRuleViolation()
    {
        var platform = SeedPendingPlatform();
        platform.Activate();
        var service = CreateService();

        var act = () => service.ActivateAsync(platform.PublicId.Value);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        platform.Status.Should().Be(PlatformStatus.Active);
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Activate_DeactivatedPlatform_ThrowsBusinessRuleViolation()
    {
        var platform = SeedPendingPlatform();
        platform.Activate();
        platform.Deactivate();
        var service = CreateService();

        var act = () => service.ActivateAsync(platform.PublicId.Value);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("invalid id with spaces")]
    [InlineData(null)]
    public async Task Activate_InvalidPublicId_ThrowsValidationException(string? publicId)
    {
        var service = CreateService();

        var act = () => service.ActivateAsync(publicId);

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Activate_UnknownPublicId_ThrowsNotFound()
    {
        var service = CreateService();

        var act = () => service.ActivateAsync(PublicId.Generate().Value);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Activate_UnderTeacherTenantContext_ThrowsTenantMismatch()
    {
        var platform = SeedPendingPlatform();
        _tenantContext.TrySetTenant(Guid.NewGuid());
        var service = CreateService();

        var act = () => service.ActivateAsync(platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
        platform.Status.Should().Be(PlatformStatus.PendingActivation);
        _unitOfWork.SaveCount.Should().Be(0);
    }
}