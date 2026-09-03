using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class CancelEnrollmentServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeEnrollmentRepository _enrollments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private CancelEnrollmentService CreateService() => new(_platforms, _students, _enrollments, _unitOfWork, _tenantContext);

    private (Student Student, TeacherPlatform Platform, Course Course) SeedActiveEnrollment(string publicId = "AbCdEf123456")
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        var course = new Course(platform.Id, "Algebra", null);
        _students.Seed(student);
        _platforms.Seed(platform);
        _enrollments.Seed(new Enrollment(student.Id, course.Id, platform.Id));
        return (student, platform, course);
    }

    [Fact]
    public async Task Cancel_ActiveEnrollment_CancelsAndSaves()
    {
        var (student, platform, course) = SeedActiveEnrollment();

        await CreateService().CancelAsync(student.Id, platform.PublicId.Value, course.Id);

        var enrollment = _enrollments.Enrollments.Should().ContainSingle().Subject;
        enrollment.Status.Should().Be(EnrollmentStatus.Cancelled);
        enrollment.CancelledAtUtc.Should().NotBeNull();
        _unitOfWork.SaveCount.Should().Be(1);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Cancel_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform, course) = SeedActiveEnrollment();

        var act = () => CreateService().CancelAsync(Guid.NewGuid(), platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_NoEnrollment_ThrowsNotFound()
    {
        var (student, platform, _) = SeedActiveEnrollment();
        var otherCourse = new Course(platform.Id, "Geometry", null);

        var act = () => CreateService().CancelAsync(student.Id, platform.PublicId.Value, otherCourse.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_InvalidPublicId_ThrowsValidation()
    {
        var (student, _, course) = SeedActiveEnrollment();

        var act = () => CreateService().CancelAsync(student.Id, "not-a-public-id", course.Id);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Cancel_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _, course) = SeedActiveEnrollment();

        var act = () => CreateService().CancelAsync(student.Id, PublicId.Generate().Value, course.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Cancel_UnderTenantContext_ThrowsTenantMismatch()
    {
        var (student, platform, course) = SeedActiveEnrollment();
        _tenantContext.TrySetTenant(platform.Id);

        var act = () => CreateService().CancelAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}
