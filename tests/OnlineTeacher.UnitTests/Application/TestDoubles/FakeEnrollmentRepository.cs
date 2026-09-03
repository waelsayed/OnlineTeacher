using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeEnrollmentRepository : IEnrollmentRepository
{
    private readonly List<Enrollment> _enrollments = [];

    public IReadOnlyList<Enrollment> Enrollments => _enrollments;

    public void Seed(Enrollment enrollment)
    {
        _enrollments.Add(enrollment);
    }

    public Task<Enrollment?> GetAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_enrollments.FirstOrDefault(e => e.StudentId == studentId && e.CourseId == courseId));

    public Task<IReadOnlyList<EnrollmentListItem>> ListByStudentForPlatformAsync(
        Guid studentId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EnrollmentListItem> result = _enrollments
            .Where(e => e.StudentId == studentId && e.TenantId == tenantId)
            .Select(e => new EnrollmentListItem(e.Id, e.CourseId, "Course", e.Status.ToString(), e.EnrolledAtUtc))
            .OrderBy(e => e.EnrolledAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<EnrollmentStudentResponse>> ListByCourseAsync(
        Guid tenantId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<EnrollmentStudentResponse> result = _enrollments
            .Where(e => e.TenantId == tenantId && e.CourseId == courseId && e.Status == EnrollmentStatus.Active)
            .Select(e => new EnrollmentStudentResponse(e.StudentId, "Student", e.EnrolledAtUtc))
            .OrderBy(e => e.EnrolledAtUtc)
            .ToList();
        return Task.FromResult(result);
    }

    public void Add(Enrollment enrollment)
    {
        _enrollments.Add(enrollment);
    }

    public void Remove(Enrollment enrollment)
    {
        _enrollments.Remove(enrollment);
    }
}
