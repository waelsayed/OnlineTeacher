using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Lightweight course entry for listing, carrying no nested structure.
/// </summary>
public sealed record CourseListItem(Guid Id, string Title, CourseStatus Status);