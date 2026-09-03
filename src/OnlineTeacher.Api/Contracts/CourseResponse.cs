using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response projection for a Course returned by create/update and listing endpoints, mapped
/// from <see cref="CourseResult"/>.
/// </summary>
public sealed record CourseResponse(Guid Id, string Title, string? Summary, string Status)
{
    public static CourseResponse From(CourseResult course) =>
        new(course.Id, course.Title, course.Summary, course.Status.ToString());
}