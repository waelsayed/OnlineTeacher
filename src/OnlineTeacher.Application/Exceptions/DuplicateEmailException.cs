namespace OnlineTeacher.Application.Exceptions;

/// <summary>
/// Raised when teacher registration conflicts with an existing email address.
/// Satisfies the duplicate-email business rule enforced by the database unique constraint.
/// </summary>
public sealed class DuplicateEmailException : BusinessRuleViolationException
{
    public DuplicateEmailException()
        : base("A teacher with this email already exists.")
    {
    }

    public DuplicateEmailException(Exception innerException)
        : base("A teacher with this email already exists.", innerException)
    {
    }
}