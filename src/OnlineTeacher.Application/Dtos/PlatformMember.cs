namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// A platform member: a teacher relationship plus the role they hold in the tenant.
/// Never exposes EF entities; used at the API/application boundary.
/// </summary>
public sealed record PlatformMember(
    Guid TeacherId,
    string TeacherName,
    Guid RoleId,
    string RoleName,
    bool IsOwner);