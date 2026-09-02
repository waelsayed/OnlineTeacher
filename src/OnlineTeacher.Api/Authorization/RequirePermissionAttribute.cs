using Microsoft.AspNetCore.Authorization;

namespace OnlineTeacher.Api.Authorization;

/// <summary>
/// Enforces a required permission code (authorization) independent of authentication.
/// Policies are resolved dynamically by <see cref="PermissionPolicyProvider"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "Permission:";

    public RequirePermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = $"{PolicyPrefix}{permission}";
    }

    public string Permission { get; }
}