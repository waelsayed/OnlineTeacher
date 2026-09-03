using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Lightweight Course entry for listing, mapped from <see cref="CourseListItem"/>.
/// </summary>
public sealed record CourseListItemResponse(Guid Id, string Title, string Status)
{
    public static CourseListItemResponse From(CourseListItem course) =>
        new(course.Id, course.Title, course.Status.ToString());
}