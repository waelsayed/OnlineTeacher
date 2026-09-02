namespace OnlineTeacher.Domain.Permissions;

/// <summary>
/// Catalog of platform permission codes used by the Role + Permission authorization model.
/// Permission codes are the canonical identifiers used for dynamic authorization.
/// </summary>
public static class PlatformPermissions
{
    public const string Access = "Platform.Access";
    public const string Manage = "Platform.Manage";

    /// <summary>
    /// Grants management of a platform's memberships (add member, change role, remove member).
    /// Owned by the platform's Owner role.
    /// </summary>
    public const string Membership = "Platform.Membership";

    public static readonly IReadOnlyCollection<string> All = new[] { Access, Manage, Membership };
}