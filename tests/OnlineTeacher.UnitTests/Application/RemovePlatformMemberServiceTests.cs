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

public class RemovePlatformMemberServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakePlatformMembershipRepository _memberships = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private RemovePlatformMemberService CreateService() => new(_platforms, _access, _memberships, _unitOfWork);

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

    [Fact]
    public async Task Remove_AssistantMember_RemovesAndSaves()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(platform, ownerId);

        var assistantId = Guid.NewGuid();
        _memberships.Seed(new TeacherPlatformMembership(ownerId, platform.Id, Guid.NewGuid(), isOwner: true), teacherName: "Owner", roleName: "Owner");
        _memberships.Seed(new TeacherPlatformMembership(assistantId, platform.Id, Guid.NewGuid(), isOwner: false), teacherName: "Assistant", roleName: "Assistant");

        await CreateService().RemoveAsync(ownerId, platform.PublicId.Value, assistantId);

        _memberships.Memberships.Should().ContainSingle(m => m.TeacherId == ownerId);
        _memberships.Memberships.Should().NotContain(m => m.TeacherId == assistantId);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Remove_LastOwner_ThrowsBusinessRuleViolation()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(platform, ownerId);

        var otherOwnerId = Guid.NewGuid();
        _access.Seed(otherOwnerId, platform.Id, new TeacherPlatformAccess(otherOwnerId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, true, ["Owner"], [PlatformPermissions.Membership]));
        _memberships.Seed(new TeacherPlatformMembership(ownerId, platform.Id, Guid.NewGuid(), isOwner: true), teacherName: "Owner", roleName: "Owner");

        var act = () => CreateService().RemoveAsync(otherOwnerId, platform.PublicId.Value, ownerId);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Remove_OwnerWhenAnotherOwnerExists_IsAllowed()
    {
        var platform = SeedPlatform(_platforms);
        var actorId = Guid.NewGuid();
        SeedOwner(platform, actorId);

        var otherOwnerId = Guid.NewGuid();
        _memberships.Seed(new TeacherPlatformMembership(actorId, platform.Id, Guid.NewGuid(), isOwner: true), teacherName: "Actor", roleName: "Owner");
        _memberships.Seed(new TeacherPlatformMembership(otherOwnerId, platform.Id, Guid.NewGuid(), isOwner: true), teacherName: "Other", roleName: "Owner");

        await CreateService().RemoveAsync(actorId, platform.PublicId.Value, otherOwnerId);

        _memberships.Memberships.Should().NotContain(m => m.TeacherId == otherOwnerId);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Remove_NonMemberTarget_ThrowsNotFound()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        SeedOwner(platform, ownerId);

        var act = () => CreateService().RemoveAsync(ownerId, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Remove_NonOwnerActor_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);
        var assistantId = Guid.NewGuid();
        _access.Seed(assistantId, platform.Id, new TeacherPlatformAccess(assistantId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, false, ["Assistant"], [PlatformPermissions.Access]));

        var act = () => CreateService().RemoveAsync(assistantId, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<TenantMismatchException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }
}