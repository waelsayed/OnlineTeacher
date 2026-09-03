using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Removes a Unit (and its Lessons) from a Course within the resolved Teacher Platform. The
/// acting teacher must be a member of the tenant.
/// </summary>
public sealed class RemoveUnitService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveUnitService(
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

    public async Task RemoveAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        Guid unitId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        var unit = course.Units.FirstOrDefault(u => u.Id == unitId)
            ?? throw new NotFoundException("Unit does not exist.");

        course.RemoveUnit(unit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}