namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /{publicId}/{slug}/api/platform/courses</c>.
/// A course is always created in Draft status; units are added separately.
/// </summary>
public sealed record CreateCourseRequest(string? Title, string? Summary);