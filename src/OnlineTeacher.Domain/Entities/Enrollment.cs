using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A tenant-scoped academic relationship between a central Student and a Teacher Platform Course.
/// Enrollment connects the central Student identity to a tenant-scoped Course. A student may have
/// multiple enrollments across different Teacher Platforms through the same central identity.
/// Following and Enrollment are separate concepts; following is not required for enrollment.
/// </summary>
public sealed class Enrollment : IAuditable, ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid StudentId { get; private set; }

    public Guid CourseId { get; private set; }

    public Guid TenantId { get; private set; }

    public EnrollmentStatus Status { get; private set; }

    public DateTime EnrolledAtUtc { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Enrollment()
    {
    }

    public Enrollment(Guid studentId, Guid courseId, Guid tenantId)
    {
        if (studentId == Guid.Empty)
        {
            throw new DomainException("Student id is required.");
        }

        if (courseId == Guid.Empty)
        {
            throw new DomainException("Course id is required.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new DomainException("Tenant id is required.");
        }

        Id = Guid.NewGuid();
        StudentId = studentId;
        CourseId = courseId;
        TenantId = tenantId;
        Status = EnrollmentStatus.Active;
        EnrolledAtUtc = DateTime.UtcNow;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Cancels an active enrollment. Only an active enrollment may be cancelled.</summary>
    public void Cancel()
    {
        if (Status != EnrollmentStatus.Active)
        {
            throw new DomainException($"Only an active enrollment can be cancelled. Current status is '{Status}'.");
        }

        Status = EnrollmentStatus.Cancelled;
        CancelledAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
