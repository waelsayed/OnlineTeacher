using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Permissions;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class CreateTeacherPlatformServiceTests
{
    private readonly FakeTeacherRepository _teachers = new();
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeRoleRepository _roles = new();
    private readonly FakePermissionRepository _permissions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private CreateTeacherPlatformService CreateService() =>
        new(_teachers, _platforms, _roles, _permissions, _unitOfWork, _tenantContext);

    private Teacher SeedTeacher()
    {
        var teacher = new Teacher("Wael Sayed", Email.Create("wael@example.com"));
        _teachers.Seed(teacher);
        return teacher;
    }

    private void SeedPermissions()
    {
        _permissions.Seed(PlatformPermissions.All.ToArray());
    }

    [Fact]
    public async Task Create_ValidInput_CreatesPendingPlatformWithOwnerRoleAndMembership()
    {
        SeedPermissions();
        var teacher = SeedTeacher();
        var service = CreateService();

        var result = await service.CreateAsync(teacher.Id, "My Platform");

        result.Status.Should().Be(PlatformStatus.PendingActivation);
        result.PublicId.Should().NotBeNullOrWhiteSpace();
        result.Slug.Should().Be("my-platform");

        var platform = _platforms.Platforms.Should().ContainSingle().Subject;
        platform.Id.Should().Be(result.PlatformId);
        platform.Status.Should().Be(PlatformStatus.PendingActivation);
        platform.ActivatedAtUtc.Should().BeNull();

        var role = _roles.Roles.Should().ContainSingle().Subject;
        role.TenantId.Should().Be(platform.Id);
        role.Name.Should().Be(PlatformRoles.Owner);
        role.Permissions.Select(rp => rp.PermissionId).Should().HaveCount(PlatformPermissions.All.Count);

        var membership = teacher.Memberships.Should().ContainSingle().Subject;
        membership.TenantId.Should().Be(platform.Id);
        membership.TeacherPlatformId.Should().Be(platform.Id);
        membership.RoleId.Should().Be(role.Id);
        membership.IsOwner.Should().BeTrue();

        _unitOfWork.SaveCount.Should().Be(1);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Create_GeneratesCryptographicPublicId()
    {
        SeedPermissions();
        var teacher = SeedTeacher();
        var service = CreateService();

        await service.CreateAsync(teacher.Id, "My Platform");

        var platform = _platforms.Platforms.Single();
        platform.PublicId.Value.Should().HaveLength(12);
        platform.PublicId.Value.Should().MatchRegex("^[0-9A-Za-z]{12}$");
    }

    [Fact]
    public async Task Create_DuplicateSlugIsAllowed()
    {
        SeedPermissions();
        var teacher1 = SeedTeacher();
        var teacher2 = new Teacher("Heba Ahmed", Email.Create("heba@example.com"));
        _teachers.Seed(teacher2);
        var service = CreateService();

        await service.CreateAsync(teacher1.Id, "My Platform");
        await service.CreateAsync(teacher2.Id, "My Platform");

        _platforms.Platforms.Should().HaveCount(2);
        _platforms.Platforms.Select(p => p.Slug.Value).Should().OnlyContain(slug => slug == "my-platform");
    }

    [Fact]
    public async Task Create_MissingTeacher_ThrowsNotFound()
    {
        SeedPermissions();
        var service = CreateService();

        var act = () => service.CreateAsync(Guid.NewGuid(), "My Platform");

        await act.Should().ThrowAsync<NotFoundException>();
        _platforms.Platforms.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_MissingPermission_ThrowsAndDoesNotSave()
    {
        var service = CreateService();
        var teacher = SeedTeacher();

        var act = () => service.CreateAsync(teacher.Id, "My Platform");

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _platforms.Platforms.Should().BeEmpty();
        _roles.Roles.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Create_EmptyName_ThrowsValidationException(string? name)
    {
        SeedPermissions();
        var teacher = SeedTeacher();
        var service = CreateService();

        var act = () => service.CreateAsync(teacher.Id, name);

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Create_UnderExistingTenantContext_ThrowsTenantMismatch()
    {
        SeedPermissions();
        var teacher = SeedTeacher();
        _tenantContext.TrySetTenant(Guid.NewGuid());
        var service = CreateService();

        var act = () => service.CreateAsync(teacher.Id, "My Platform");

        await act.Should().ThrowAsync<TenantMismatchException>();
        _platforms.Platforms.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }
}