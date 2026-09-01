namespace OnlineTeacher.Application.Exceptions;

/// <summary>
/// Raised when an operation runs in the wrong tenant scope.
/// </summary>
public class TenantMismatchException : Exception
{
    public TenantMismatchException(string message)
        : base(message)
    {
    }

    public TenantMismatchException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}