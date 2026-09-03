using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Lists the Courses within the resolved Teacher Platform. The acting teacher must be a member
/// of the tenant; the list is ordered by course title.
/// </summary>
public sealed class ListCoursesService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;

    public ListCoursesService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        ICourseRepository courses)
    {
        _platforms = platforms;
        _access = access;
        _courses = courses;
    }

    public async Task<IReadOnlyList<CourseListItem>> ListAsync(
        Guid actorTeacherId,
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var courses = await _courses.ListAsync(platform.Id, cancellationToken);

        return courses
            .Select(c => new CourseListItem(c.Id, c.Title, c.Status))
            .ToList();
    }
}