using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Deletes a Course within the resolved Teacher Platform along with its Units and Lessons.
/// The acting teacher must be a member of the tenant.
/// </summary>
public sealed class DeleteCourseService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCourseService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        ICourseRepository courses,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _courses = courses;
        _unitOfWork = unitOfWork;
    }

    public async Task DeleteAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        _courses.Remove(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}