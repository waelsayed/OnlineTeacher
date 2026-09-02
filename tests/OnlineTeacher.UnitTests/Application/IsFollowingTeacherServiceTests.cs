using FluentAssertions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class IsFollowingTeacherServiceTests
{
    private readonly FakeStudentRepository _students = new();
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakePlatformMembershipRepository _memberships = new();
    private readonly FakeStudentFollowRepository _follows = new();
    private readonly StubTenantContext _tenantContext = new();

    private IsFollowingTeacherService CreateService() => new(_students, _platforms, _memberships, _follows, _tenantContext);

    private (Student Student, TeacherPlatform Platform, Teacher Owner) SeedTarget(string publicId)
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var owner = new Teacher("Teacher", Email.Create($"teacher-{Guid.NewGuid():N}@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        var role = new Role(platform.Id, "Owner");
        var membership = new TeacherPlatformMembership(owner.Id, platform.Id, role.Id, isOwner: true);

        _students.Seed(student);
        _platforms.Seed(platform);
        _memberships.Seed(membership, owner.Name, "Owner");

        return (student, platform, owner);
    }

    [Fact]
    public async Task IsFollowing_Following_ReturnsTrue()
    {
        var (student, platform, owner) = SeedTarget("AbCdEf123456");
        _follows.Seed(new StudentFollow(student.Id, owner.Id));

        var result = await CreateService().IsFollowingAsync(student.Id, platform.PublicId.Value);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsFollowing_NotFollowing_ReturnsFalse()
    {
        var (student, platform, _) = SeedTarget("ZzYyXx987654");

        var result = await CreateService().IsFollowingAsync(student.Id, platform.PublicId.Value);

        result.Should().BeFalse();
    }
}