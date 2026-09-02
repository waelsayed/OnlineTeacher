namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>POST /api/central/platforms/{publicId}/activate</c>.
/// </summary>
public sealed record ActivateTeacherPlatformResponse(Guid PlatformId, string PublicId, DateTime ActivatedAtUtc);