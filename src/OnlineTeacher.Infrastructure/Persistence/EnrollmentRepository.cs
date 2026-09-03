using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for the tenant-scoped Student Enrollment relationship.
/// Duplicate (Student, Course) pairs are rejected by a database unique constraint.
/// </summary>
public sealed class EnrollmentRepository : IEnrollmentRepository
{
    private readonly ApplicationDbContext _db;

    public EnrollmentRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Enrollment?> GetAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken = default) =>
        _db.Enrollments.FirstOrDefaultAsync(
            e => e.StudentId == studentId && e.CourseId == courseId,
            cancellationToken);

    public async Task<IReadOnlyList<EnrollmentListItem>> ListByStudentForPlatformAsync(
        Guid studentId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Enrollments
            .Where(e => e.StudentId == studentId && e.TenantId == tenantId)
            .Join(_db.Courses,
                e => e.CourseId,
                c => c.Id,
                (e, c) => new { Enrollment = e, Course = c })
            .OrderBy(x => x.Enrollment.EnrolledAtUtc)
            .Select(x => new { x.Enrollment.Id, CourseId = x.Course.Id, x.Course.Title, x.Enrollment.Status, x.Enrollment.EnrolledAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new EnrollmentListItem(x.Id, x.CourseId, x.Title, x.Status.ToString(), x.EnrolledAtUtc))
            .ToList();
    }

    public async Task<IReadOnlyList<EnrollmentStudentResponse>> ListByCourseAsync(
        Guid tenantId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Enrollments
            .Where(e => e.TenantId == tenantId && e.CourseId == courseId && e.Status == EnrollmentStatus.Active)
            .Join(_db.Students,
                e => e.StudentId,
                s => s.Id,
                (e, s) => new { Enrollment = e, Student = s })
            .OrderBy(x => x.Enrollment.EnrolledAtUtc)
            .Select(x => new { x.Student.Id, x.Student.Name, x.Enrollment.EnrolledAtUtc })
            .ToListAsync(cancellationToken);

        return rows
            .Select(x => new EnrollmentStudentResponse(x.Id, x.Name, x.EnrolledAtUtc))
            .ToList();
    }

    public void Add(Enrollment enrollment)
    {
        _db.Enrollments.Add(enrollment);
    }

    public void Remove(Enrollment enrollment)
    {
        _db.Enrollments.Remove(enrollment);
    }
}
