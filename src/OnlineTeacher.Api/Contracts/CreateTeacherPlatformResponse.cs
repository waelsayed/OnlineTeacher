namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>POST /api/central/platforms</c>.
/// </summary>
public sealed record CreateTeacherPlatformResponse(Guid PlatformId, string PublicId, string Slug, string Status);