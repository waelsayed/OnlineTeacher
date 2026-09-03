using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Summary projection of a Course for listing and create/update responses. Public-safe and
/// carries no internal persistence details.
/// </summary>
public sealed record CourseResult(Guid Id, string Title, string? Summary, CourseStatus Status);