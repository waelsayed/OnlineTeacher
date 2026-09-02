namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /api/student/register</c>. No Teacher Platform publicId is
/// required because student identity is central.
/// </summary>
public sealed record RegisterStudentRequest(string? Name, string? Email, string? Password);