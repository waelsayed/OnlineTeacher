using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Adds a Lesson to a Unit within the resolved Teacher Platform. The lesson is appended at the
/// end of the unit's ordering unless an explicit positive position is supplied. The acting
/// teacher must be a member of the tenant.
/// </summary>
public sealed class AddLessonService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public AddLessonService(
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

    public async Task<CourseLesson> AddAsync(
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

        Lesson lesson;
        try
        {
            lesson = position is null
                ? unit.AddLesson(title ?? string.Empty)
                : unit.AddLesson(title ?? string.Empty, position.Value);
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        _courses.AddLesson(lesson);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CourseLesson(lesson.Id, lesson.Title, lesson.Position);
    }
}