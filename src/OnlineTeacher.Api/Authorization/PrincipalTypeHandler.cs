using Microsoft.AspNetCore.Authorization;
using OnlineTeacher.Api.Authentication;

namespace OnlineTeacher.Api.Authorization;

/// <summary>
/// Grants the requirement when the authenticated user is of the required principal type as
/// declared by the trusted <c>principal_type</c> claim. A Student JWT therefore never
/// satisfies Teacher-only policies and a Teacher JWT never satisfies Student-only policies.
/// </summary>
public sealed class PrincipalTypeHandler : AuthorizationHandler<PrincipalTypeRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PrincipalTypeRequirement requirement)
    {
        var principalType = context.User.FindFirst(JwtClaims.PrincipalType)?.Value;

        if (string.Equals(principalType, requirement.PrincipalType, StringComparison.Ordinal))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}