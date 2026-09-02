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

public class ChangePlatformMemberRoleServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakePlatformMembershipRepository _memberships = new();
    private readonly FakeRoleRepository _roles = new();
    private readonly FakeTeacherRepository _teachers = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private ChangePlatformMemberRoleService CreateService() =>
        new(_platforms, _access, _memberships, _roles, _teachers, _unitOfWork);

    private static TeacherPlatform SeedPlatform(FakePlatformRepository platforms)
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create("AbCdEf123456"), Slug.CreateFromName("my-platform"));
        platforms.Seed(platform);
        return platform;
    }

    private void SeedOwner(TeacherPlatform platform, Guid ownerId)
    {
        _access.Seed(ownerId, platform.Id, new TeacherPlatformAccess(ownerId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, true, ["Owner"], [PlatformPermissions.Membership]));
    }

    private void SeedRoles(TeacherPlatform platform)
    {
        _roles.Seed(new Role(platform.Id, PlatformRoles.Owner));
        _roles.Seed(new Role(platform.Id, PlatformRoles.Assistant));
    }

    [Fact]
    public async Task Change_DemoteAssistantToAssistant_KeepsOwnerAlone()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(platform, ownerId);
        SeedRoles(platform);

        var assistantId = Guid.NewGuid();
        var assistantRoleId = _roles.Roles.Single(r => r.Name == PlatformRoles.Assistant).Id;
        _teachers.Seed(new Teacher("Assistant", Email.Create("assistant@example.com")));
        _memberships.Seed(new TeacherPlatformMembership(ownerId, platform.Id, _roles.Roles.Single(r => r.Name == PlatformRoles.Owner).Id, isOwner: true), teacherName: "Owner", roleName: PlatformRoles.Owner);
        _memberships.Seed(new TeacherPlatformMembership(assistantId, platform.Id, assistantRoleId, isOwner: false), teacherName: "Assistant", roleName: PlatformRoles.Assistant);

        var result = await CreateService().ChangeAsync(ownerId, platform.PublicId.Value, assistantId, PlatformRoles.Owner);

        result.IsOwner.Should().BeTrue();
        var membership = _memberships.Memberships.Single(m => m.TeacherId == assistantId);
        membership.IsOwner.Should().BeTrue();
        membership.RoleId.Should().Be(_roles.Roles.Single(r => r.Name == PlatformRoles.Owner).Id);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Change_DemoteLastOwner_ThrowsBusinessRuleViolation()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(platform, ownerId);
        SeedRoles(platform);

        var actorId = Guid.NewGuid();
        _access.Seed(actorId, platform.Id, new TeacherPlatformAccess(actorId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, true, ["Owner"], [PlatformPermissions.Membership]));

        var ownerRoleId = _roles.Roles.Single(r => r.Name == PlatformRoles.Owner).Id;
        _teachers.Seed(new Teacher("Owner", Email.Create("owner@example.com")));
        _memberships.Seed(new TeacherPlatformMembership(ownerId, platform.Id, ownerRoleId, isOwner: true), teacherName: "Owner", roleName: PlatformRoles.Owner);

        var act = () => CreateService().ChangeAsync(actorId, platform.PublicId.Value, ownerId, PlatformRoles.Assistant);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Change_DemoteOwnerWithAnotherOwner_IsAllowed()
    {
        var platform = SeedPlatform(_platforms);
        var actorId = Guid.NewGuid();
        SeedOwner(platform, actorId);
        SeedRoles(platform);

        var otherOwnerId = Guid.NewGuid();
        var ownerRoleId = _roles.Roles.Single(r => r.Name == PlatformRoles.Owner).Id;
        _teachers.Seed(new Teacher("Other", Email.Create("other@example.com")));
        _memberships.Seed(new TeacherPlatformMembership(actorId, platform.Id, ownerRoleId, isOwner: true), teacherName: "Actor", roleName: PlatformRoles.Owner);
        _memberships.Seed(new TeacherPlatformMembership(otherOwnerId, platform.Id, ownerRoleId, isOwner: true), teacherName: "Other", roleName: PlatformRoles.Owner);

        var act = () => CreateService().ChangeAsync(actorId, platform.PublicId.Value, otherOwnerId, PlatformRoles.Assistant);

        await act.Should().NotThrowAsync();
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Change_MemberNotInPlatform_ThrowsNotFound()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(platform, ownerId);
        SeedRoles(platform);

        var act = () => CreateService().ChangeAsync(ownerId, platform.PublicId.Value, Guid.NewGuid(), PlatformRoles.Assistant);

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Change_UnknownRole_ThrowsValidationException()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(platform, ownerId);

        var targetId = Guid.NewGuid();
        _teachers.Seed(new Teacher("Target", Email.Create("target@example.com")));
        _memberships.Seed(new TeacherPlatformMembership(targetId, platform.Id, Guid.NewGuid(), isOwner: false), teacherName: "Target", roleName: "Unknown");

        var act = () => CreateService().ChangeAsync(ownerId, platform.PublicId.Value, targetId, "NotARole");

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Change_NonOwnerActor_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);
        var assistantId = Guid.NewGuid();
        _access.Seed(assistantId, platform.Id, new TeacherPlatformAccess(assistantId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, false, ["Assistant"], [PlatformPermissions.Access]));

        var act = () => CreateService().ChangeAsync(assistantId, platform.PublicId.Value, Guid.NewGuid(), PlatformRoles.Assistant);

        await act.Should().ThrowAsync<TenantMismatchException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }
}