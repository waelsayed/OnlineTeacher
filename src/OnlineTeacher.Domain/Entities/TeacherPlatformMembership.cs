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

    /// <summary>
    /// Changes the member's role and/or owner flag.
    ///
    /// Preventing orphaning the platform (removing its last owner) is an aggregate-wide
    /// invariant that depends on the set of memberships, so it is enforced by the
    /// application layer, not here.
    /// </summary>
    public void ChangeRole(Guid newRoleId, bool isOwner)
    {
        if (newRoleId == Guid.Empty)
        {
            throw new DomainException("Membership requires a role.");
        }

        RoleId = newRoleId;
        IsOwner = isOwner;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}