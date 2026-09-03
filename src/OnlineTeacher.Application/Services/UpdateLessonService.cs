using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Renames or moves a Lesson within a Unit in the resolved Teacher Platform. The acting teacher
/// must be a member of the tenant.
/// </summary>
public sealed class UpdateLessonService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateLessonService(
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

    public async Task<CourseLesson> UpdateAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        Guid unitId,
        Guid lessonId,
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

        var lesson = unit.Lessons.FirstOrDefault(l => l.Id == lessonId)
            ?? throw new NotFoundException("Lesson does not exist.");

        try
        {
            lesson.Rename(title);

            if (position is not null)
            {
                unit.MoveLesson(lesson, position.Value);
            }
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CourseLesson(lesson.Id, lesson.Title, lesson.Position);
    }
}