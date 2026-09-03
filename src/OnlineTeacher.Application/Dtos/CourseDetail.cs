using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Full Course structure including its ordered Units and their ordered Lessons. This is the
/// public-safe projection used for detailed course reads; internal ids are not exposed.
/// </summary>
public sealed record CourseDetail(
    Guid Id,
    string Title,
    string? Summary,
    CourseStatus Status,
    IReadOnlyList<CourseUnit> Units);