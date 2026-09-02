using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Central Platform teacher-registration operations. These run under no tenant context
/// and are kept open to any caller (registration is the bootstrap path for a teacher
/// identity prior to any login).
/// </summary>
[ApiController]
[Route("api/central/teachers")]
[AllowAnonymous]
public sealed class TeachersController : ControllerBase
{
    private readonly RegisterTeacherService _registerTeacher;

    public TeachersController(RegisterTeacherService registerTeacher)
    {
        _registerTeacher = registerTeacher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterTeacherRequest request, CancellationToken cancellationToken)
    {
        var result = await _registerTeacher.RegisterAsync(request.Name, request.Email, request.Password, cancellationToken);
        return Created(string.Empty, new RegisterTeacherResponse(result.TeacherId));
    }
}