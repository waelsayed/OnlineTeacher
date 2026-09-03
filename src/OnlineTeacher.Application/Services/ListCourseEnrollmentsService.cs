using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Lists the students enrolled in a Course for a Teacher Platform. The acting teacher must be a
/// member of the tenant; the Enrollment.View permission is enforced by the API's permission policy.
/// Only active enrollments are listed, ordered by enrollment date.
/// </summary>
public sealed class ListCourseEnrollmentsService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IEnrollmentRepository _enrollments;

    public ListCourseEnrollmentsService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        ICourseRepository courses,
        IEnrollmentRepository enrollments)
    {
        _platforms = platforms;
        _access = access;
        _courses = courses;
        _enrollments = enrollments;
    }

    public async Task<IReadOnlyList<EnrollmentStudentResponse>> ListAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        return await _enrollments.ListByCourseAsync(platform.Id, courseId, cancellationToken);
    }
}
