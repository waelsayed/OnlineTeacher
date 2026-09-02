using Microsoft.AspNetCore.Authorization;
using OnlineTeacher.Api.Authentication;

namespace OnlineTeacher.Api.Authorization;

/// <summary>
/// Grants the requirement when the authenticated user's trusted permission claims contain
/// the required code. An authenticated user without the claim is denied (403); authentication
/// is handled separately by the JWT bearer scheme so an unauthenticated request is 401.
/// </summary>
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var hasPermission = context.User.Claims.Any(c =>
            c.Type == PermissionClaims.Type &&
            string.Equals(c.Value, requirement.Permission, StringComparison.Ordinal));

        if (hasPermission)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}