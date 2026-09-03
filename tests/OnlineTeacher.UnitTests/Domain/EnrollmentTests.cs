using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class EnrollmentTests
{
    private static Enrollment NewEnrollment(
        Guid? studentId = null,
        Guid? courseId = null,
        Guid? tenantId = null) =>
        new(
            studentId ?? Guid.NewGuid(),
            courseId ?? Guid.NewGuid(),
            tenantId ?? Guid.NewGuid());

    [Fact]
    public void Create_SetsIdsAndDefaults()
    {
        var studentId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var enrollment = NewEnrollment(studentId, courseId, tenantId);

        enrollment.StudentId.Should().Be(studentId);
        enrollment.CourseId.Should().Be(courseId);
        enrollment.TenantId.Should().Be(tenantId);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);
        enrollment.CancelledAtUtc.Should().BeNull();
        enrollment.EnrolledAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_RejectsEmptyStudentId()
    {
        var act = () => new Enrollment(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyCourseId()
    {
        var act = () => new Enrollment(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyTenantId()
    {
        var act = () => new Enrollment(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Cancel_ActiveEnrollment_SetsCancelledStatus()
    {
        var enrollment = NewEnrollment();

        enrollment.Cancel();

        enrollment.Status.Should().Be(EnrollmentStatus.Cancelled);
        enrollment.CancelledAtUtc.Should().NotBeNull();
        enrollment.CancelledAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        enrollment.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_Throws()
    {
        var enrollment = NewEnrollment();
        enrollment.Cancel();

        var act = () => enrollment.Cancel();

        act.Should().Throw<DomainException>()
            .Which.Message.Should().Contain("cancelled");
    }
}
