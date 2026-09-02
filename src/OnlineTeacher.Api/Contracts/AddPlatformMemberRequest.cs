namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /{publicId}/{slug}/api/platform/members</c>.
/// The teacher is identified by their central email address.
/// </summary>
public sealed record AddPlatformMemberRequest(string Email);