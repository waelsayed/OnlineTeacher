namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Outcome of a Teacher Platform activation.
/// </summary>
public sealed record ActivateTeacherPlatformResult(
    Guid PlatformId,
    string PublicId,
    DateTime ActivatedAtUtc);