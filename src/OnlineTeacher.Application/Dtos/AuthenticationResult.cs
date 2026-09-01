namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Outcome of a central teacher authentication attempt.
/// Password/hash details are never exposed through this result.
/// </summary>
public sealed record AuthenticationResult(bool Succeeded, Guid? TeacherId, string? FailureMessage)
{
    public static AuthenticationResult Ok(Guid teacherId) => new(true, teacherId, null);

    public static AuthenticationResult Failed => new(false, null, "Invalid email or password.");
}