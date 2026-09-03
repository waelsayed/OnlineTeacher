namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// A Unit within a Course detail, with its ordered Lessons. Carries the internal id needed to
/// address the unit in subsequent management routes.
/// </summary>
public sealed record CourseUnit(Guid Id, string Title, int Position, IReadOnlyList<CourseLesson> Lessons);

/// <summary>
/// A Lesson within a CourseUnit detail. Carries the internal id needed to address the lesson in
/// subsequent management routes.
/// </summary>
public sealed record CourseLesson(Guid Id, string Title, int Position);