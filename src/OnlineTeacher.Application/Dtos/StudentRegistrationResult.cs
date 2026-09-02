namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Outcome of a successful student registration.
/// </summary>
public sealed record StudentRegistrationResult(Guid StudentId);