namespace OnlineTeacher.Domain.Permissions;

/// <summary>
/// Catalog of platform permission codes used by the Role + Permission authorization model.
/// Permission codes are the canonical identifiers used for dynamic authorization.
/// </summary>
public static class PlatformPermissions
{
    public const string Access = "Platform.Access";
    public const string Manage = "Platform.Manage";

    public static readonly IReadOnlyCollection<string> All = new[] { Access, Manage };
}