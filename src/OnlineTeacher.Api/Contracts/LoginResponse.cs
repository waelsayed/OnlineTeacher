namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>POST /api/auth/login</c>. Contains only non-sensitive identity data
/// plus the bearer token; never password/hash material.
/// </summary>
public sealed record LoginResponse(string Token, Guid TeacherId, string? PlatformId, string? Slug);