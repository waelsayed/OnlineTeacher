using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Updates the editable fields and status of a Course within the resolved tenant. Only title,
/// summary, and the Draft/Published status are mutable; the internal id and tenant are immutable.
/// The acting teacher must be a member of the tenant.
/// </summary>
public sealed class UpdateCourseService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateCourseService(
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

    public async Task<CourseResult> UpdateAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid courseId,
        string? title,
        string? summary,
        CourseStatus? status,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        try
        {
            course.Update(title, summary);

            if (status is not null)
            {
                ApplyStatus(course, status.Value);
            }
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CourseResult(course.Id, course.Title, course.Summary, course.Status);
    }

    private static void ApplyStatus(Course course, CourseStatus status)
    {
        switch (status)
        {
            case CourseStatus.Published:
                course.Publish();
                break;
            case CourseStatus.Draft:
                course.ToDraft();
                break;
            default:
                throw new ValidationException($"Unknown course status '{status}'.");
        }
    }
}