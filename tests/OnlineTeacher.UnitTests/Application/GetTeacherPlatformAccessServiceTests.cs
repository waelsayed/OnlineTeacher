using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class GetTeacherPlatformAccessServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly StubTenantContext _tenantContext = new();

    private GetTeacherPlatformAccessService CreateService() =>
        new(_platforms, _access, _tenantContext);

    private static TeacherPlatform SeedPlatform(FakePlatformRepository platforms, string publicId = "AbCdEf123456", string slug = "my-platform")
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName(slug));
        platforms.Seed(platform);
        return platform;
    }

    private static TeacherPlatformAccess AccessFor(TeacherPlatform platform, Guid? teacherId = null, bool isOwner = true,
        string[]? roleNames = null, string[]? permissions = null) =>
        new(
            teacherId ?? Guid.NewGuid(),
            platform.Id,
            platform.PublicId.Value,
            platform.Slug.Value,
            PlatformStatus.Active,
            isOwner,
            roleNames ?? ["Owner"],
            permissions ?? ["Platform.Access", "Platform.Manage"]);

    [Fact]
    public async Task Get_ValidMember_ReturnsAccessAndRestoresCentralContext()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        var access = AccessFor(platform, teacherId, roleNames: ["Owner"], permissions: ["Platform.Access"]);
        _access.Seed(teacherId, platform.Id, access);

        var result = await CreateService().GetAsync(teacherId, platform.PublicId.Value);

        result.TeacherId.Should().Be(teacherId);
        result.PlatformId.Should().Be(platform.Id);
        result.PublicId.Should().Be(platform.PublicId.Value);
        result.Status.Should().Be(PlatformStatus.Active);
        result.RoleNames.Should().Equal("Owner");
        result.PermissionCodes.Should().Equal("Platform.Access");
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Get_InvalidPublicId_ThrowsValidationException()
    {
        var act = () => CreateService().GetAsync(Guid.NewGuid(), "not-a-public-id");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Get_UnknownPublicId_ThrowsNotFound()
    {
        var act = () => CreateService().GetAsync(Guid.NewGuid(), PublicId.Generate().Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_NotAMember_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);

        var act = () => CreateService().GetAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Get_UnderDifferentActiveTenant_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);
        _tenantContext.TrySetTenant(Guid.NewGuid());
        var service = CreateService();

        var act = () => service.GetAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task Get_UnderSameActiveTenant_RunsWithoutRestoring()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, AccessFor(platform));
        _tenantContext.TrySetTenant(platform.Id);
        var service = CreateService();

        var result = await service.GetAsync(teacherId, platform.PublicId.Value);

        result.Should().NotBeNull();
        _tenantContext.TenantId.Should().Be(platform.Id);
    }

    [Fact]
    public void Access_Model_HasNoPasswordOrHashProperty()
    {
        var properties = typeof(TeacherPlatformAccess).GetProperties().Select(p => p.Name).ToArray();

        properties.Should().NotContain("PasswordHash");
        properties.Should().NotContain("Password");
    }
}