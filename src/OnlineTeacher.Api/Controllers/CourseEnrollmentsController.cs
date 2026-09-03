using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Teacher Platform course-enrollment reads. The route-template parameters {publicId} and {slug}
/// are validated by TenantRouteMiddleware, which resolves and scopes the tenant. Listing the
/// students enrolled in a course requires the <c>Enrollment.View</c> permission. The application
/// service further enforces that the acting user is a member of the resolved tenant, so a valid
/// cross-tenant JWT cannot read another teacher's enrollments.
/// </summary>
[ApiController]
[Route("{publicId}/{slug}/api/platform/courses/{courseId:guid}/enrollments")]
[Authorize]
public sealed class CourseEnrollmentsController : ControllerBase
{
    private readonly ListCourseEnrollmentsService _listCourseEnrollments;

    public CourseEnrollmentsController(ListCourseEnrollmentsService listCourseEnrollments)
    {
        _listCourseEnrollments = listCourseEnrollments;
    }

    [HttpGet]
    [RequirePermission("Enrollment.View")]
    public async Task<IActionResult> List(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        CancellationToken cancellationToken)
    {
        var students = await _listCourseEnrollments.ListAsync(GetTeacherIdClaim(), publicId, courseId, cancellationToken);
        return Ok(students.Select(CourseEnrolledStudentResponse.From));
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
