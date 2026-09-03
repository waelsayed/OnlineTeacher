namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /{publicId}/{slug}/api/platform/courses/{courseId}/units</c>.
/// When no position is supplied the unit is appended at the end of the course's ordering.
/// </summary>
public sealed record AddUnitRequest(string? Title, int? Position);