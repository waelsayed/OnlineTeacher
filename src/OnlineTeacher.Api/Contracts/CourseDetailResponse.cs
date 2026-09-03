using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Detailed Course projection including its ordered Units and their Lessons, mapped from
/// <see cref="CourseDetail"/>.
/// </summary>
public sealed record CourseDetailResponse(
    Guid Id,
    string Title,
    string? Summary,
    string Status,
    IReadOnlyList<CourseUnitResponse> Units)
{
    public static CourseDetailResponse From(CourseDetail course) =>
        new(
            course.Id,
            course.Title,
            course.Summary,
            course.Status.ToString(),
            course.Units.Select(CourseUnitResponse.From).ToList());
}

/// <summary>Projection of a Unit within a course detail.</summary>
public sealed record CourseUnitResponse(Guid Id, string Title, int Position, IReadOnlyList<CourseLessonResponse> Lessons)
{
    public static CourseUnitResponse From(CourseUnit unit) =>
        new(
            unit.Id,
            unit.Title,
            unit.Position,
            unit.Lessons.Select(CourseLessonResponse.From).ToList());
}

/// <summary>Projection of a Lesson within a unit detail.</summary>
public sealed record CourseLessonResponse(Guid Id, string Title, int Position)
{
    public static CourseLessonResponse From(CourseLesson lesson) =>
        new(lesson.Id, lesson.Title, lesson.Position);
}