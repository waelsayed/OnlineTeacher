namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>PUT /{publicId}/{slug}/api/platform/courses/{courseId}/units/{unitId}</c>.
/// Fields are optional; only supplied fields are changed. A position moves the unit.
/// </summary>
public sealed record UpdateUnitRequest(string? Title, int? Position);