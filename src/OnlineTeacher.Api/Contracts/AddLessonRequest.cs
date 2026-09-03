namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /{publicId}/{slug}/api/platform/courses/{courseId}/units/{unitId}/lessons</c>.
/// When no position is supplied the lesson is appended at the end of the unit's ordering.
/// </summary>
public sealed record AddLessonRequest(string? Title, int? Position);