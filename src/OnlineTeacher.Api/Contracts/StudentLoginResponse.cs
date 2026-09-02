namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>POST /api/student/login</c>. Contains only non-sensitive identity data
/// (a central, tenant-agnostic student JWT) and never password/hash material.
/// </summary>
public sealed record StudentLoginResponse(string Token, Guid StudentId);