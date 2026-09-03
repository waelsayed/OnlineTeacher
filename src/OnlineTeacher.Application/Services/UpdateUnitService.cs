using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Renames or moves a Unit within a Course in the resolved Teacher Platform. The acting teacher
/// must be a member of the tenant.
/// </summary>
public sealed class UpdateUnitService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUnitService(
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

    public async Task<CourseUnit> UpdateAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        Guid unitId,
        string? title,
        int? position,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        var unit = course.Units.FirstOrDefault(u => u.Id == unitId)
            ?? throw new NotFoundException("Unit does not exist.");

        try
        {
            unit.Rename(title);

            if (position is not null)
            {
                course.MoveUnit(unit, position.Value);
            }
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lessons = unit.Lessons
            .OrderBy(l => l.Position)
            .Select(l => new CourseLesson(l.Id, l.Title, l.Position))
            .ToList();

        return new CourseUnit(unit.Id, unit.Title, unit.Position, lessons);
    }
}