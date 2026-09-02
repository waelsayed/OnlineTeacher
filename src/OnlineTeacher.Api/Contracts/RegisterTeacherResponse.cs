namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>POST /api/central/teachers/register</c>.
/// </summary>
public sealed record RegisterTeacherResponse(Guid TeacherId);