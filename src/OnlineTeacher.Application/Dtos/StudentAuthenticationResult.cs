namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Outcome of a central student authentication attempt. Password/hash details are never
/// exposed through this result, and failure stays generic to avoid account enumeration.
/// </summary>
public sealed record StudentAuthenticationResult(bool Succeeded, Guid? StudentId, string? FailureMessage)
{
    public static StudentAuthenticationResult Ok(Guid studentId) => new(true, studentId, null);

    public static StudentAuthenticationResult Failed => new(false, null, "Invalid email or password.");
}