namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /api/central/platforms</c>.
/// </summary>
public sealed record CreateTeacherPlatformRequest(Guid TeacherId, string? Name);