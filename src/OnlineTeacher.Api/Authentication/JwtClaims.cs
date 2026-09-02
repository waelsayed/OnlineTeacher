using System.Security.Claims;

namespace OnlineTeacher.Api.Authentication;

/// <summary>
/// Defines the approved JWT claim names. Claims are limited to the identity and
/// authorization data required for tenant-scoped access; passwords/hashes/sensitive
/// member data are never placed in tokens.
/// </summary>
public static class JwtClaims
{
    public const string Tenant = "tenant";
    public const string Permission = "permission";
    public const string Role = ClaimTypes.Role;
    public const string IsOwner = "isOwner";
}

/// <summary>
/// Well-known permission claims treated as trusted server-generated identities.
/// Used by the permission policy handler to read the permission claims.
/// </summary>
public static class PermissionClaims
{
    public const string Type = JwtClaims.Permission;
}