namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>PUT /{publicId}/{slug}/api/platform/courses/{courseId}</c>.
/// Fields are optional; only supplied fields are changed. Status accepts the raw
/// course-status value (e.g. <c>Published</c>, <c>Draft</c>).
/// </summary>
public sealed record UpdateCourseRequest(string? Title, string? Summary, string? Status);