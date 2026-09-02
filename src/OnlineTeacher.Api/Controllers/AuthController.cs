using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Central authentication. Login is anonymous (any caller may authenticate) and returns a
/// platform-scoped JWT. Generates the token only after a successful, non-sensitive
/// application authentication; failure is kept generic to avoid email/account enumeration.
/// </summary>
[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public sealed class AuthController : ControllerBase
{
    private readonly AuthenticateTeacherService _authenticate;
    private readonly GetTeacherPlatformAccessService _getAccess;
    private readonly JwtTokenFactory _jwt;

    public AuthController(
        AuthenticateTeacherService authenticate,
        GetTeacherPlatformAccessService getAccess,
        JwtTokenFactory jwt)
    {
        _authenticate = authenticate;
        _getAccess = getAccess;
        _jwt = jwt;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var authentication = await _authenticate.AuthenticateAsync(request.Email, request.Password, cancellationToken);

        if (!authentication.Succeeded || authentication.TeacherId is null)
        {
            return Unauthorized(new
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = authentication.FailureMessage
            });
        }

        var access = await _getAccess.GetAsync(authentication.TeacherId.Value, request.PublicId, cancellationToken);

        var response = new LoginResponse(
            _jwt.Create(access),
            access.TeacherId,
            access.PublicId,
            access.Slug);

        return Ok(response);
    }
}