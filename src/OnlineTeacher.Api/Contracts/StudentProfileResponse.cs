namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>GET /api/student/me</c>. A public-safe projection of the student; the
/// password hash is never exposed.
/// </summary>
public sealed record StudentProfileResponse(Guid StudentId, string Name, string Email);