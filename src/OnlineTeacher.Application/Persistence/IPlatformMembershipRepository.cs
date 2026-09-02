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
}