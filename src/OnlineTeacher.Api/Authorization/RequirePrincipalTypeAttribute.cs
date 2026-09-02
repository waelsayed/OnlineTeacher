using Microsoft.AspNetCore.Authorization;

namespace OnlineTeacher.Api.Authorization;

/// <summary>
/// Enforces that the authenticated principal is of the given type (e.g. teacher or student).
/// Policies are resolved dynamically by <see cref="PermissionPolicyProvider"/> so no up-front
/// policy registration is required.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePrincipalTypeAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "PrincipalType:";

    public RequirePrincipalTypeAttribute(string principalType)
    {
        PrincipalType = principalType;
        Policy = $"{PolicyPrefix}{principalType}";
    }

    public string PrincipalType { get; }
}