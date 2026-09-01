namespace OnlineTeacher.Application.Exceptions;

/// <summary>
/// Raised when the request input failed boundary validation.
/// </summary>
public class ValidationException : Exception
{
    public ValidationException(string message)
        : base(message)
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}