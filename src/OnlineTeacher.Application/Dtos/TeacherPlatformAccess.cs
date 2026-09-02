using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// A teacher's access profile within a Teacher Platform: the membership's roles and the
/// permission codes those roles grant. Used to construct platform-scoped JWT claims and
/// to verify tenant-access authorization server-side. Never contains password/hash data.
/// </summary>
public sealed record TeacherPlatformAccess(
    Guid TeacherId,
    Guid PlatformId,
    string PublicId,
    string Slug,
    PlatformStatus Status,
    bool IsOwner,
    IReadOnlyList<string> RoleNames,
    IReadOnlyList<string> PermissionCodes);