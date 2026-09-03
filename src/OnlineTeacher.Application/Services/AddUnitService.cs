using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Adds a Unit to a Course within the resolved Teacher Platform. The unit is appended at the end
/// of the course's ordering unless an explicit positive position is supplied. The acting teacher
/// must be a member of the tenant.
/// </summary>
public sealed class AddUnitService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public AddUnitService(
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

    public async Task<CourseUnit> AddAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        string? title,
        int? position,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        Unit unit;
        try
        {
            unit = position is null
                ? course.AddUnit(title ?? string.Empty)
                : course.AddUnit(title ?? string.Empty, position.Value);
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        _courses.AddUnit(unit);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var lessons = unit.Lessons
            .OrderBy(l => l.Position)
            .Select(l => new CourseLesson(l.Id, l.Title, l.Position))
            .ToList();

        return new CourseUnit(unit.Id, unit.Title, unit.Position, lessons);
    }
}