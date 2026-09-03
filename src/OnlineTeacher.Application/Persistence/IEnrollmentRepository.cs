using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for the tenant-scoped Student Enrollment relationship.
/// Duplicate (Student, Course) pairs are prevented by a database unique constraint.
/// </summary>
public interface IEnrollmentRepository
{
    Task<Enrollment?> GetAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentListItem>> ListByStudentForPlatformAsync(Guid studentId, Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EnrollmentStudentResponse>> ListByCourseAsync(Guid tenantId, Guid courseId, CancellationToken cancellationToken = default);

    void Add(Enrollment enrollment);

    void Remove(Enrollment enrollment);
}

/// <summary>Projection for a student's enrollment list scoped to a platform.</summary>
public sealed record EnrollmentListItem(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseTitle,
    string Status,
    DateTime EnrolledAtUtc);

/// <summary>Projection for a teacher's view of enrolled students in a course.</summary>
public sealed record EnrollmentStudentResponse(
    Guid StudentId,
    string StudentName,
    DateTime EnrolledAtUtc);
