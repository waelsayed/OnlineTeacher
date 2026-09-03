namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>PUT /{publicId}/{slug}/api/platform/courses/{courseId}/units/{unitId}/lessons/{lessonId}</c>.
/// Fields are optional; only supplied fields are changed. A position moves the lesson.
/// </summary>
public sealed record UpdateLessonRequest(string? Title, int? Position);