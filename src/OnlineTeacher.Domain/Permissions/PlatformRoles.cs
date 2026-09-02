namespace OnlineTeacher.Domain.Permissions;

/// <summary>
/// Catalog of fixed role names. Platform ownership is represented through
/// a TeacherPlatformMembership associated with an Owner role.
/// </summary>
public static class PlatformRoles
{
    public const string Owner = "Owner";

    /// <summary>
    /// Non-owner member role used when the owner adds additional members to a platform.
    /// Granted only a minimal permission set; never carries ownership.
    /// </summary>
    public const string Assistant = "Assistant";
}