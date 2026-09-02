using Microsoft.AspNetCore.Authorization;

namespace OnlineTeacher.Api.Authorization;

/// <summary>
/// Requires the authenticated principal to be of a specific type (teacher or student),
/// enforced via the trusted <c>principal_type</c> JWT claim.
/// </summary>
public sealed class PrincipalTypeRequirement : IAuthorizationRequirement
{
    public PrincipalTypeRequirement(string principalType)
    {
        PrincipalType = principalType;
    }

    public string PrincipalType { get; }
}