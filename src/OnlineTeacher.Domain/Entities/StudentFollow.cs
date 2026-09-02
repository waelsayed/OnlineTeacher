using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A central, non-tenant-scoped relationship between a Student and a Teacher.
/// The relationship is independent of any Teacher Platform tenant and never grants
/// access to private Teacher Platform management endpoints (enrollment is a later concern).
/// </summary>
public sealed class StudentFollow : IAuditable
{
    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid TeacherId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private StudentFollow()
    {
    }

    public StudentFollow(Guid studentId, Guid teacherId)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (teacherId == Guid.Empty)
        {
            throw new DomainException("Teacher id is required.");
        }

        if (studentId == teacherId)
        {
            throw new DomainException("A student cannot follow themselves.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        TeacherId = teacherId;
        CreatedAtUtc = DateTime.UtcNow;
    }
}