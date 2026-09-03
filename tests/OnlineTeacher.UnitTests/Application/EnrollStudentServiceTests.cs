using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class EnrollStudentServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeEnrollmentRepository _enrollments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private EnrollStudentService CreateService() => new(_platforms, _students, _courses, _enrollments, _unitOfWork, _tenantContext);

    private (Student Student, TeacherPlatform Platform) SeedEligibleTarget(string publicId = "AbCdEf123456")
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        platform.Activate();
        _students.Seed(student);
        _platforms.Seed(platform);
        return (student, platform);
    }

    private Course SeedPublishedCourse(TeacherPlatform platform)
    {
        var course = new Course(platform.Id, "Algebra", "An algebra course.");
        course.Publish();
        _courses.Seed(course);
        return course;
    }

    [Fact]
    public async Task Enroll_ValidEligibleTarget_CreatesActiveEnrollment()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPublishedCourse(platform);

        var id = await CreateService().EnrollAsync(student.Id, platform.PublicId.Value, course.Id);

        var enrollment = _enrollments.Enrollments.Should().ContainSingle().Subject;
        enrollment.Id.Should().Be(id);
        enrollment.StudentId.Should().Be(student.Id);
        enrollment.CourseId.Should().Be(course.Id);
        enrollment.TenantId.Should().Be(platform.Id);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        _unitOfWork.SaveCount.Should().Be(1);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Enroll_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform) = SeedEligibleTarget();
        var course = SeedPublishedCourse(platform);

        var act = () => CreateService().EnrollAsync(Guid.NewGuid(), platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Enroll_InvalidPublicId_ThrowsValidation()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().EnrollAsync(student.Id, "not-a-public-id", Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Enroll_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().EnrollAsync(student.Id, PublicId.Generate().Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Enroll_InactivePlatform_ThrowsBusinessRuleViolation()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create("AbCdEf123456"), Slug.CreateFromName("my-platform"));
        _students.Seed(student);
        _platforms.Seed(platform);

        var act = () => CreateService().EnrollAsync(student.Id, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Enroll_UnknownCourse_ThrowsNotFound()
    {
        var (student, platform) = SeedEligibleTarget();

        var act = () => CreateService().EnrollAsync(student.Id, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Enroll_UnpublishedCourse_ThrowsBusinessRuleViolation()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = new Course(platform.Id, "Algebra", "An algebra course.");
        _courses.Seed(course);

        var act = () => CreateService().EnrollAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*published*");
    }

    [Fact]
    public async Task Enroll_AlreadyEnrolled_ThrowsBusinessRuleViolation()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPublishedCourse(platform);
        _enrollments.Seed(new Enrollment(student.Id, course.Id, platform.Id));

        var act = () => CreateService().EnrollAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*already enrolled*");
    }

    [Fact]
    public async Task Enroll_UnderTenantContext_ThrowsTenantMismatch()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPublishedCourse(platform);
        _tenantContext.TrySetTenant(platform.Id);

        var act = () => CreateService().EnrollAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}
