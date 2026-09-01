namespace OnlineTeacher.Application.Exceptions;

/// <summary>
/// Raised when a requested entity or resource does not exist.
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}