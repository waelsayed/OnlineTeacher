using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Creates a Course in Draft status within the resolved Teacher Platform. The acting teacher
/// must hold a membership in the tenant; the Course.Manage permission is enforced by the API's
/// permission policy. A new course is created draft with no units. A Paid course requires a
/// positive price (EGP); a Free course carries no price. A provided pricing type is applied
/// additively while preserving the default Free behavior.
/// </summary>
public sealed class CreateCourseService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ICourseRepository _courses;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCourseService(
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

    public async Task<CourseResult> CreateAsync(
        Guid actorTeacherId,
        string? publicId,
        string? title,
        string? summary,
        CoursePricingType? pricingType = null,
        decimal? price = null,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        Course course;
        try
        {
            course = new Course(platform.Id, title ?? string.Empty, summary);

            if (pricingType is not null)
            {
                course.SetPricing(pricingType.Value, price);
            }
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        _courses.Add(course);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CourseResult(course.Id, course.Title, course.Summary, course.Status);
    }
}
