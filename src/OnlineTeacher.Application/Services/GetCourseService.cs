using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Reads a single Course (with its ordered Units and Lessons) within the resolved Teacher
/// Platform. The acting teacher must be a member of the tenant.
/// </summary>
public sealed class GetCourseService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;

    public GetCourseService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        ICourseRepository courses)
    {
        _platforms = platforms;
        _access = access;
        _courses = courses;
    }

    public async Task<CourseDetail> GetAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        return Map(course);
    }

    private static CourseDetail Map(Course course)
    {
        var units = course.Units
            .OrderBy(u => u.Position)
            .Select(u => new CourseUnit(
                u.Id,
                u.Title,
                u.Position,
                u.Lessons
                    .OrderBy(l => l.Position)
                    .Select(l => new CourseLesson(l.Id, l.Title, l.Position))
                    .ToList()))
            .ToList();

        return new CourseDetail(course.Id, course.Title, course.Summary, course.Status, units);
    }
}