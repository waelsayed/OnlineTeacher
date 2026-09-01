namespace OnlineTeacher.Application.Exceptions;

/// <summary>
/// Raised when an optimistic concurrency conflict is detected during persistence.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message)
        : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}