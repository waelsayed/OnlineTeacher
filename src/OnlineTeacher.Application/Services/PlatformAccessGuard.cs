using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Enforces that an acting teacher holds a valid membership (and, where required, ownership)
/// within the resolved tenant before a management operation proceeds. Reused by the
/// management use cases as a defense-in-depth backstop on top of permission policies:
/// a JWT may carry a management permission, but the actor must also be a member of the
/// specific platform being managed, so cross-tenant management is impossible.
/// </summary>
internal static class PlatformAccessGuard
{
    public static async Task<TeacherPlatformAccess> RequireMemberAsync(
        ITeacherPlatformAccessRepository access,
        Guid actorTeacherId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var accessProfile = await access.GetAsync(actorTeacherId, tenantId, cancellationToken);
        return accessProfile
            ?? throw new TenantMismatchException("The teacher is not a member of this platform.");
    }

    public static async Task<TeacherPlatformAccess> RequireOwnerAsync(
        ITeacherPlatformAccessRepository access,
        Guid actorTeacherId,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var accessProfile = await RequireMemberAsync(access, actorTeacherId, tenantId, cancellationToken);

        if (!accessProfile.IsOwner)
        {
            throw new TenantMismatchException("Only a platform owner can perform this operation.");
        }

        return accessProfile;
    }
}