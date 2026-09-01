using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Outcome of a Teacher Platform creation.
/// </summary>
public sealed record CreateTeacherPlatformResult(
    Guid PlatformId,
    string PublicId,
    string Slug,
    PlatformStatus Status);