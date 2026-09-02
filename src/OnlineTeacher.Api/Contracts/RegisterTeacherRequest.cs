namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /api/central/teachers/register</c>.
/// </summary>
public sealed record RegisterTeacherRequest(string? Name, string? Email, string? Password);