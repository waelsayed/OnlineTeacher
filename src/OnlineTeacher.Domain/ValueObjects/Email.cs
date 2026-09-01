using System.Text.RegularExpressions;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.ValueObjects;

/// <summary>
/// Basic email address value object with lightweight format validation.
/// </summary>
public sealed record Email
{
    private static readonly Regex FormatPattern =
        new("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Email is required.");
        }

        var trimmed = value.Trim();

        if (!FormatPattern.IsMatch(trimmed))
        {
            throw new DomainException("Email format is invalid.");
        }

        return new Email(trimmed);
    }

    public override string ToString() => Value;
}