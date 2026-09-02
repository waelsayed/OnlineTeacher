using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>GET /{publicId}/{slug}/api/platform/me</c>. A non-sensitive projection of
/// the authenticated teacher's access within the resolved tenant.
/// </summary>
public sealed record PlatformMeResponse(
    string TenantPublicId,
    string Slug,
    string Status,
    bool IsOwner,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<string> PermissionCodes)
{
    public static PlatformMeResponse From(TeacherPlatformAccess access) =>
        new(
            access.PublicId,
            access.Slug,
            access.Status switch
            {
                PlatformStatus.PendingActivation => "PendingActivation",
                PlatformStatus.Active => "Active",
                PlatformStatus.Deactivated => "Deactivated",
                _ => access.Status.ToString()
            },
            access.IsOwner,
            access.RoleNames,
            access.PermissionCodes);
}