using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ListFollowedTeachersServiceTests
{
    private readonly FakeStudentRepository _students = new();
    private readonly FakeStudentFollowRepository _follows = new();
    private readonly FakePlatformMembershipRepository _memberships = new();

    private ListFollowedTeachersService CreateService() => new(_students, _follows, _memberships);

    [Fact]
    public async Task List_StudentFollowingMultipleTeachers_ReturnsTheirPlatforms()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        _students.Seed(student);

        var teacherA = new Teacher("Teacher A", Email.Create("a@example.com"));
        var teacherB = new Teacher("Teacher B", Email.Create("b@example.com"));
        var platformA = new TeacherPlatform("Platform A", PublicId.Create("Aaaaaaaa1111"), Slug.CreateFromName("platform-a"));
        var platformB = new TeacherPlatform("Platform B", PublicId.Create("Bbbbbbbb2222"), Slug.CreateFromName("platform-b"));
        var roleA = new Role(platformA.Id, "Owner");
        var roleB = new Role(platformB.Id, "Owner");

        _memberships.Seed(new TeacherPlatformMembership(teacherA.Id, platformA.Id, roleA.Id, isOwner: true));
        _memberships.SeedOwnedPlatform(
            platformA.Id,
            new OwnedPlatform(platformA.PublicId.Value, platformA.Slug.Value));
        _memberships.Seed(new TeacherPlatformMembership(teacherB.Id, platformB.Id, roleB.Id, isOwner: true));
        _memberships.SeedOwnedPlatform(
            platformB.Id,
            new OwnedPlatform(platformB.PublicId.Value, platformB.Slug.Value));

        _follows.Seed(new StudentFollow(student.Id, teacherA.Id));
        _follows.Seed(new StudentFollow(student.Id, teacherB.Id));

        var result = await CreateService().ListAsync(student.Id);

        result.Select(f => f.PublicId).Should().Contain(new[] { platformA.PublicId.Value, platformB.PublicId.Value });
        result.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task List_StudentFollowingNoOne_ReturnsEmpty()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        _students.Seed(student);

        var result = await CreateService().ListAsync(student.Id);

        result.Should().BeEmpty();
    }
}