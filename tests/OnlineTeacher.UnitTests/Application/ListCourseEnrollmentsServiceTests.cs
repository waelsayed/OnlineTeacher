using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ListCourseEnrollmentsServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeEnrollmentRepository _enrollments = new();

    private ListCourseEnrollmentsService CreateService() => new(_platforms, _access, _courses, _enrollments);

    private (TeacherPlatform Platform, Guid TeacherId) SeedMember(string publicId = "AbCdEf123456")
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        _platforms.Seed(platform);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, new TeacherPlatformAccess(
            teacherId,
            platform.Id,
            platform.PublicId.Value,
            platform.Slug.Value,
            PlatformStatus.Active,
            true,
            ["Owner"],
            ["Platform.Access", "Platform.Manage"]));
        return (platform, teacherId);
    }

    private Course SeedCourse(TeacherPlatform platform)
    {
        var course = new Course(platform.Id, "Algebra", "An algebra course.");
        _courses.Seed(course);
        return course;
    }

    [Fact]
    public async Task List_ReturnsActiveEnrolledStudents()
    {
        var (platform, teacherId) = SeedMember();
        var course = SeedCourse(platform);
        var studentA = new Student("Sara", Email.Create("sara@example.com"));
        var studentB = new Student("Ali", Email.Create("ali@example.com"));
        _enrollments.Seed(new Enrollment(studentA.Id, course.Id, platform.Id));
        _enrollments.Seed(new Enrollment(studentB.Id, course.Id, platform.Id));

        var result = await CreateService().ListAsync(teacherId, platform.PublicId.Value, course.Id);

        result.Should().HaveCount(2);
        result.Select(e => e.StudentId).Should().Contain(studentA.Id).And.Contain(studentB.Id);
    }

    [Fact]
    public async Task List_ExcludesCancelledEnrollments()
    {
        var (platform, teacherId) = SeedMember();
        var course = SeedCourse(platform);
        var studentA = new Student("Sara", Email.Create("sara@example.com"));
        var studentB = new Student("Ali", Email.Create("ali@example.com"));
        var active = new Enrollment(studentA.Id, course.Id, platform.Id);
        var cancelled = new Enrollment(studentB.Id, course.Id, platform.Id);
        cancelled.Cancel();
        _enrollments.Seed(active);
        _enrollments.Seed(cancelled);

        var result = await CreateService().ListAsync(teacherId, platform.PublicId.Value, course.Id);

        result.Should().ContainSingle();
        result[0].StudentId.Should().Be(studentA.Id);
    }

    [Fact]
    public async Task List_NonMemberActor_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();
        var course = SeedCourse(platform);

        var act = () => CreateService().ListAsync(Guid.NewGuid(), platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task List_UnknownCourse_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();

        var act = () => CreateService().ListAsync(teacherId, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task List_InvalidPublicId_ThrowsValidation()
    {
        var (_, teacherId) = SeedMember();

        var act = () => CreateService().ListAsync(teacherId, "not-a-public-id", Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task List_UnknownPlatform_ThrowsNotFound()
    {
        var (_, teacherId) = SeedMember();

        var act = () => CreateService().ListAsync(teacherId, PublicId.Generate().Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
