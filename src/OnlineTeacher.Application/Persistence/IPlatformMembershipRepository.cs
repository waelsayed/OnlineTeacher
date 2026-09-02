using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Purpose-specific data access for Teacher Platform memberships within one tenant.
/// Queries are tenant-scoped: callers must establish the tenant context so the EF query
/// filters keep reads isolated to the active platform.
/// </summary>
public interface IPlatformMembershipRepository
{
    /// <summary>
    /// Returns the members of the given tenant. Request must run with the tenant established.
    /// </summary>
    Task<IReadOnlyList<PlatformMember>> GetMembersAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single teacher's membership in the given tenant, or null when none exists.
    /// </summary>
    Task<TeacherPlatformMembership?> GetForTeacherAsync(Guid tenantId, Guid teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a membership for removal. Actual deletion is committed by the unit of work.
    /// </summary>
    void Remove(TeacherPlatformMembership membership);

    /// <summary>
    /// Returns the tenant's owner teacher Id, or null when the tenant has no owner.
    /// Request must run with the tenant established.
    /// </summary>
    Task<Guid?> GetOwnerTeacherIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the platforms (public identity + slug) for which the given teacher is the owner.
    /// This is a central/explicit read used to present a student's followed teachers as
    /// browseable public platforms; it is deliberately scoped to one teacher.
    /// </summary>
    Task<IReadOnlyList<OwnedPlatform>> GetOwnedPlatformsAsync(Guid teacherId, CancellationToken cancellationToken = default);
}