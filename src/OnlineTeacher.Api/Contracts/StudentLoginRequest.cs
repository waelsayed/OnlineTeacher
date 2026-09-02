namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /api/student/login</c>. Student identity is central, so no
/// Teacher Platform publicId is required.
/// </summary>
public sealed record StudentLoginRequest(string? Email, string? Password);