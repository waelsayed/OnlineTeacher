namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for <c>POST /api/student/register</c>. Contains only the central student Id.
/// </summary>
public sealed record RegisterStudentResponse(Guid StudentId);