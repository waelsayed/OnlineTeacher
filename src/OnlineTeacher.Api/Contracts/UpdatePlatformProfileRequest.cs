namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>PUT/PATCH /{publicId}/{slug}/api/platform/profile</c>.
/// At least one of Name or Slug must be supplied; both are validated by the domain.
/// The internal Id and PublicId are never editable.
/// </summary>
public sealed record UpdatePlatformProfileRequest(string? Name, string? Slug);