using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Authenticated Teacher Platform access. The route-template parameters {publicId} and
/// {slug} are validated by TenantRouteMiddleware; the controller accesses them normally
/// via [FromRoute]. Requires the Platform.Access permission that is only granted to the
/// tenant's own membership, so a valid token for another tenant is denied.
/// </summary>
[ApiController]
[Route("{publicId}/{slug}/api/platform")]
[Authorize]
public sealed class PlatformController : ControllerBase
{
    private readonly GetTeacherPlatformAccessService _getAccess;

    public PlatformController(GetTeacherPlatformAccessService getAccess)
    {
        _getAccess = getAccess;
    }

    [HttpGet("me")]
    [RequirePermission("Platform.Access")]
    public async Task<IActionResult> Me(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var teacherId = GetTeacherIdClaim();

        var access = await _getAccess.GetAsync(teacherId, publicId, cancellationToken);

        return Ok(PlatformMeResponse.From(access));
    }

    private Guid GetTeacherIdClaim()
    {
        var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        var raw = sub?.Value;
        if (raw is null || !Guid.TryParse(raw, out var teacherId))
        {
            throw new UnauthorizedAccessException("The token does not carry a valid teacher identity.");
        }

        return teacherId;
    }
}