namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// A student's public-safe profile. Never contains the password hash.
/// </summary>
public sealed record StudentProfile(Guid StudentId, string Name, string Email);