using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Permissions;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class AddPlatformMemberServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakePlatformMembershipRepository _memberships = new();
    private readonly FakeTeacherRepository _teachers = new();
    private readonly FakeRoleRepository _roles = new();
    private readonly FakePermissionRepository _permissions = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private AddPlatformMemberService CreateService() =>
        new(_platforms, _access, _memberships, _teachers, _roles, _permissions, _unitOfWork);

    private static TeacherPlatform SeedPlatform(FakePlatformRepository platforms)
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create("AbCdEf123456"), Slug.CreateFromName("my-platform"));
        platforms.Seed(platform);
        return platform;
    }

    private Teacher SeedTeacher(Guid id, string email)
    {
        var teacher = new Teacher("Teacher", Email.Create(email));
        _teachers.Seed(teacher);
        return teacher;
    }

    private void SeedOwner(FakePlatformRepository platforms, Guid ownerId)
    {
        var platform = platforms.Platforms.Single();
        _access.Seed(ownerId, platform.Id, new TeacherPlatformAccess(ownerId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, true, ["Owner"], [PlatformPermissions.Membership]));
    }

    [Fact]
    public async Task Add_ValidTeacher_CreatesAssistantMembershipAndSaves()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(_platforms, ownerId);
        var newMember = SeedTeacher(Guid.NewGuid(), "new@example.com");
        _permissions.Seed(PlatformPermissions.Access);

        var result = await CreateService().AddAsync(ownerId, platform.PublicId.Value, "new@example.com");

        result.TeacherId.Should().Be(newMember.Id);
        result.IsOwner.Should().BeFalse();
        result.RoleName.Should().Be(PlatformRoles.Assistant);

        var assistantRole = _roles.Roles.Should().ContainSingle().Subject;
        assistantRole.Name.Should().Be(PlatformRoles.Assistant);
        assistantRole.TenantId.Should().Be(platform.Id);

        var membership = _teachers.Memberships.Should().ContainSingle().Subject;
        membership.TeacherId.Should().Be(newMember.Id);
        membership.TeacherPlatformId.Should().Be(platform.Id);
        membership.RoleId.Should().Be(assistantRole.Id);
        membership.IsOwner.Should().BeFalse();

        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Add_UnknownTeacherEmail_ThrowsValidationException()
    {
        SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(_platforms, ownerId);

        var act = () => CreateService().AddAsync(ownerId, "AbCdEf123456", "nobody@example.com");

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Add_InvalidEmail_ThrowsValidationException()
    {
        SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(_platforms, ownerId);

        var act = () => CreateService().AddAsync(ownerId, "AbCdEf123456", "not-an-email");

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Add_OwnerAddingSelf_ThrowsBusinessRuleViolation()
    {
        SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(_platforms, ownerId);
        _teachers.Seed(new Teacher("Owner", Email.Create("owner@example.com")));

        var act = () => CreateService().AddAsync(ownerId, "AbCdEf123456", "owner@example.com");

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Add_AlreadyMember_ThrowsBusinessRuleViolation()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(_platforms, ownerId);
        var existing = SeedTeacher(Guid.NewGuid(), "member@example.com");

        var assistantRoleId = Guid.NewGuid();
        _memberships.Seed(new TeacherPlatformMembership(existing.Id, platform.Id, assistantRoleId, isOwner: false), teacherName: "Member", roleName: "Assistant");

        var act = () => CreateService().AddAsync(ownerId, platform.PublicId.Value, "member@example.com");

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Add_NonOwnerActor_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);
        var assistantId = Guid.NewGuid();
        _access.Seed(assistantId, platform.Id, new TeacherPlatformAccess(assistantId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, false, ["Assistant"], [PlatformPermissions.Access]));
        _teachers.Seed(new Teacher("Target", Email.Create("target@example.com")));

        var act = () => CreateService().AddAsync(assistantId, platform.PublicId.Value, "target@example.com");

        await act.Should().ThrowAsync<TenantMismatchException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Add_MissingAccessPermission_ThrowsBusinessRuleViolation()
    {
        SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(_platforms, ownerId);
        SeedTeacher(Guid.NewGuid(), "new@example.com");

        var act = () => CreateService().AddAsync(ownerId, "AbCdEf123456", "new@example.com");

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }
}