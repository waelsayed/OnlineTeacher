using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ListPlatformMembersServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakePlatformMembershipRepository _memberships = new();

    private ListPlatformMembersService CreateService() => new(_platforms, _access, _memberships);

    private static TeacherPlatform SeedPlatform(FakePlatformRepository platforms)
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create("AbCdEf123456"), Slug.CreateFromName("my-platform"));
        platforms.Seed(platform);
        return platform;
    }

    [Fact]
    public async Task List_AuthorizedMember_ReturnsMembers()
    {
        var platform = SeedPlatform(_platforms);
        var ownerId = Guid.NewGuid();
        var assistantId = Guid.NewGuid();
        _access.Seed(ownerId, platform.Id, new TeacherPlatformAccess(ownerId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, true, ["Owner"], ["Platform.Manage"]));

        var ownerRoleId = Guid.NewGuid();
        var assistantRoleId = Guid.NewGuid();
        _memberships.Seed(new TeacherPlatformMembership(ownerId, platform.Id, ownerRoleId, isOwner: true), teacherName: "Owner", roleName: "Owner");
        _memberships.Seed(new TeacherPlatformMembership(assistantId, platform.Id, assistantRoleId, isOwner: false), teacherName: "Assistant", roleName: "Assistant");

        var result = await CreateService().ListAsync(ownerId, platform.PublicId.Value);

        result.Should().HaveCount(2);
        result.Should().Contain(m => m.IsOwner && m.TeacherId == ownerId);
        result.Should().Contain(m => !m.IsOwner && m.TeacherId == assistantId && m.RoleName == "Assistant");
    }

    [Fact]
    public async Task List_NonMemberActor_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);

        var act = () => CreateService().ListAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}