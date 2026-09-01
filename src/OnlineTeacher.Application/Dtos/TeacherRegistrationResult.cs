namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Outcome of a successful teacher registration.
/// </summary>
public sealed record TeacherRegistrationResult(Guid TeacherId);