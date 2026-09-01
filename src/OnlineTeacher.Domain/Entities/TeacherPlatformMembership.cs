using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// Represents a teacher's relationship to a Teacher Platform through a role.
/// Platform ownership is represented through this membership model (Owner role).
/// A teacher has at most one membership per platform.
/// </summary>
public sealed class TeacherPlatformMembership : IAuditable, ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid TeacherId { get; private set; }

    public Guid TeacherPlatformId { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid RoleId { get; private set; }

    public bool IsOwner { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private TeacherPlatformMembership()
    {
    }

    public TeacherPlatformMembership(Guid teacherId, Guid teacherPlatformId, Guid roleId, bool isOwner = false)
    {
        if (teacherId == Guid.Empty)
        {
            throw new DomainException("Membership requires a teacher.");
        }

        if (teacherPlatformId == Guid.Empty)
        {
            throw new DomainException("Membership requires a teacher platform.");
        }

        if (roleId == Guid.Empty)
        {
            throw new DomainException("Membership requires a role.");
        }

        Id = Guid.NewGuid();
        TeacherId = teacherId;
        TeacherPlatformId = teacherPlatformId;
        TenantId = teacherPlatformId;
        RoleId = roleId;
        IsOwner = isOwner;
        CreatedAtUtc = DateTime.UtcNow;
    }
}