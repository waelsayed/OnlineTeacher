using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Central Platform teacher-platform lifecycle operations. These run under no tenant
/// context. Activation requires the central Platform.Manage permission.
/// </summary>
[ApiController]
[Route("api/central/platforms")]
public sealed class CentralPlatformsController : ControllerBase
{
    private readonly CreateTeacherPlatformService _createPlatform;
    private readonly ActivateTeacherPlatformService _activatePlatform;

    public CentralPlatformsController(
        CreateTeacherPlatformService createPlatform,
        ActivateTeacherPlatformService activatePlatform)
    {
        _createPlatform = createPlatform;
        _activatePlatform = activatePlatform;
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateTeacherPlatformRequest request, CancellationToken cancellationToken)
    {
        var result = await _createPlatform.CreateAsync(request.TeacherId, request.Name, cancellationToken);
        return Created(string.Empty, new CreateTeacherPlatformResponse(
            result.PlatformId, result.PublicId, result.Slug, result.Status.ToString()));
    }

    [HttpPost("{publicId}/activate")]
    [RequirePermission("Platform.Manage")]
    public async Task<IActionResult> Activate(string publicId, CancellationToken cancellationToken)
    {
        var result = await _activatePlatform.ActivateAsync(publicId, cancellationToken);
        return Ok(new ActivateTeacherPlatformResponse(result.PlatformId, result.PublicId, result.ActivatedAtUtc));
    }
}