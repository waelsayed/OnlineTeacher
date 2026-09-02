using FluentAssertions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class UnfollowTeacherServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakePlatformMembershipRepository _memberships = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeStudentFollowRepository _follows = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private UnfollowTeacherService CreateService() => new(_platforms, _memberships, _students, _follows, _unitOfWork, _tenantContext);

    private (Student Student, TeacherPlatform Platform, Teacher Owner) SeedFollowing(
        string publicId = "AbCdEf123456")
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var owner = new Teacher("Teacher", Email.Create($"teacher-{Guid.NewGuid():N}@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        var role = new Role(platform.Id, "Owner");
        var membership = new TeacherPlatformMembership(owner.Id, platform.Id, role.Id, isOwner: true);

        _students.Seed(student);
        _platforms.Seed(platform);
        _memberships.Seed(membership, owner.Name, "Owner");
        _follows.Seed(new StudentFollow(student.Id, owner.Id));

        return (student, platform, owner);
    }

    [Fact]
    public async Task Unfollow_ExistingFollow_RemovesFollowAndCommits()
    {
        var (student, platform, owner) = SeedFollowing();

        await CreateService().UnfollowAsync(student.Id, platform.PublicId.Value);

        _follows.Follows.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(1);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Unfollow_NotFollowing_IsSafeNoOp()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var owner = new Teacher("Teacher", Email.Create($"teacher-{Guid.NewGuid():N}@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create("ZzYyXx987654"), Slug.CreateFromName("my-platform"));
        var role = new Role(platform.Id, "Owner");
        var membership = new TeacherPlatformMembership(owner.Id, platform.Id, role.Id, isOwner: true);
        _students.Seed(student);
        _platforms.Seed(platform);
        _memberships.Seed(membership, owner.Name, "Owner");

        await CreateService().UnfollowAsync(student.Id, platform.PublicId.Value);

        _follows.Follows.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }
}