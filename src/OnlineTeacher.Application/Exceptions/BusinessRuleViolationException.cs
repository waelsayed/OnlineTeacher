namespace OnlineTeacher.Application.Exceptions;

/// <summary>
/// Raised when a business rule is violated.
/// </summary>
public class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message)
        : base(message)
    {
    }

    public BusinessRuleViolationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}