using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Authenticated Teacher Platform course-content management (Courses → Units → Lessons).
/// The route-template parameters {publicId} and {slug} are validated by TenantRouteMiddleware,
/// which resolves and scopes the tenant. Reads require <c>Course.View</c>; mutations require
/// <c>Course.Manage</c>. Application services further enforce that the acting user is a member
/// of the resolved tenant, so a valid cross-tenant JWT cannot read or manage another teacher's
/// course content.
/// </summary>
[ApiController]
[Route("{publicId}/{slug}/api/platform/courses")]
[Authorize]
public sealed class CourseContentController : ControllerBase
{
    private readonly ListCoursesService _listCourses;
    private readonly GetCourseService _getCourse;
    private readonly CreateCourseService _createCourse;
    private readonly UpdateCourseService _updateCourse;
    private readonly DeleteCourseService _deleteCourse;
    private readonly AddUnitService _addUnit;
    private readonly UpdateUnitService _updateUnit;
    private readonly RemoveUnitService _removeUnit;
    private readonly AddLessonService _addLesson;
    private readonly UpdateLessonService _updateLesson;
    private readonly RemoveLessonService _removeLesson;

    public CourseContentController(
        ListCoursesService listCourses,
        GetCourseService getCourse,
        CreateCourseService createCourse,
        UpdateCourseService updateCourse,
        DeleteCourseService deleteCourse,
        AddUnitService addUnit,
        UpdateUnitService updateUnit,
        RemoveUnitService removeUnit,
        AddLessonService addLesson,
        UpdateLessonService updateLesson,
        RemoveLessonService removeLesson)
    {
        _listCourses = listCourses;
        _getCourse = getCourse;
        _createCourse = createCourse;
        _updateCourse = updateCourse;
        _deleteCourse = deleteCourse;
        _addUnit = addUnit;
        _updateUnit = updateUnit;
        _removeUnit = removeUnit;
        _addLesson = addLesson;
        _updateLesson = updateLesson;
        _removeLesson = removeLesson;
    }

    [HttpGet]
    [RequirePermission("Course.View")]
    public async Task<IActionResult> List(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var courses = await _listCourses.ListAsync(GetTeacherIdClaim(), publicId, cancellationToken);
        return Ok(courses.Select(CourseListItemResponse.From));
    }

    [HttpGet("{courseId}")]
    [RequirePermission("Course.View")]
    public async Task<IActionResult> Get(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        CancellationToken cancellationToken)
    {
        var course = await _getCourse.GetAsync(GetTeacherIdClaim(), publicId, courseId, cancellationToken);
        return Ok(CourseDetailResponse.From(course));
    }

    [HttpPost]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> Create(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromBody] CreateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await _createCourse.CreateAsync(
            GetTeacherIdClaim(), publicId, request.Title, request.Summary, cancellationToken);
        return Ok(CourseResponse.From(course));
    }

    [HttpPut("{courseId}")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> Update(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        [FromBody] UpdateCourseRequest request,
        CancellationToken cancellationToken)
    {
        var course = await _updateCourse.UpdateAsync(
            GetTeacherIdClaim(), publicId, courseId, request.Title, request.Summary,
            ParseStatus(request.Status), cancellationToken);
        return Ok(CourseResponse.From(course));
    }

    [HttpDelete("{courseId}")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> Delete(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        CancellationToken cancellationToken)
    {
        await _deleteCourse.DeleteAsync(GetTeacherIdClaim(), publicId, courseId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{courseId}/units")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> AddUnit(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        [FromBody] AddUnitRequest request,
        CancellationToken cancellationToken)
    {
        var unit = await _addUnit.AddAsync(
            GetTeacherIdClaim(), publicId, courseId, request.Title, request.Position, cancellationToken);
        return Ok(CourseUnitResponse.From(unit));
    }

    [HttpPut("{courseId}/units/{unitId}")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> UpdateUnit(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        [FromRoute] Guid unitId,
        [FromBody] UpdateUnitRequest request,
        CancellationToken cancellationToken)
    {
        var unit = await _updateUnit.UpdateAsync(
            GetTeacherIdClaim(), publicId, courseId, unitId, request.Title, request.Position, cancellationToken);
        return Ok(CourseUnitResponse.From(unit));
    }

    [HttpDelete("{courseId}/units/{unitId}")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> RemoveUnit(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        [FromRoute] Guid unitId,
        CancellationToken cancellationToken)
    {
        await _removeUnit.RemoveAsync(GetTeacherIdClaim(), publicId, courseId, unitId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{courseId}/units/{unitId}/lessons")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> AddLesson(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        [FromRoute] Guid unitId,
        [FromBody] AddLessonRequest request,
        CancellationToken cancellationToken)
    {
        var lesson = await _addLesson.AddAsync(
            GetTeacherIdClaim(), publicId, courseId, unitId, request.Title, request.Position, cancellationToken);
        return Ok(CourseLessonResponse.From(lesson));
    }

    [HttpPut("{courseId}/units/{unitId}/lessons/{lessonId}")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> UpdateLesson(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        [FromRoute] Guid unitId,
        [FromRoute] Guid lessonId,
        [FromBody] UpdateLessonRequest request,
        CancellationToken cancellationToken)
    {
        var lesson = await _updateLesson.UpdateAsync(
            GetTeacherIdClaim(), publicId, courseId, unitId, lessonId, request.Title, request.Position, cancellationToken);
        return Ok(CourseLessonResponse.From(lesson));
    }

    [HttpDelete("{courseId}/units/{unitId}/lessons/{lessonId}")]
    [RequirePermission("Course.Manage")]
    public async Task<IActionResult> RemoveLesson(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid courseId,
        [FromRoute] Guid unitId,
        [FromRoute] Guid lessonId,
        CancellationToken cancellationToken)
    {
        await _removeLesson.RemoveAsync(GetTeacherIdClaim(), publicId, courseId, unitId, lessonId, cancellationToken);
        return NoContent();
    }

    private static CourseStatus? ParseStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        if (!Enum.TryParse<CourseStatus>(status, ignoreCase: true, out var parsed))
        {
            throw new OnlineTeacher.Application.Exceptions.ValidationException($"Unknown course status '{status}'.");
        }

        return parsed;
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