using System.Text;
using System.Text.RegularExpressions;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.ValueObjects;

/// <summary>
/// URL-safe, canonical, normalized slug used as an SEO/canonical component of a Teacher Platform URL.
/// Slug is NOT globally unique and must never be treated as primary platform identity.
/// </summary>
public sealed record Slug
{
    private const int MaxLength = 60;
    private const string Fallback = "platform";

    private static readonly Regex CanonicalPattern =
        new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<char> AllowedCharacters = new("abcdefghijklmnopqrstuvwxyz0123456789");

    public string Value { get; }

    private Slug(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Normalizes a platform name into a canonical, deterministic, URL-safe slug.
    /// </summary>
    public static Slug CreateFromName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Platform name is required to create a slug.");
        }

        var builder = new StringBuilder(name.Length);
        char? lastAppended = null;

        foreach (var character in name.Trim().ToLowerInvariant())
        {
            if (AllowedCharacters.Contains(character))
            {
                builder.Append(character);
                lastAppended = character;
            }
            else if (lastAppended != '-')
            {
                builder.Append('-');
                lastAppended = '-';
            }
        }

        var slug = builder.ToString().Trim('-');

        if (slug.Length == 0)
        {
            slug = Fallback;
        }

        if (slug.Length > MaxLength)
        {
            slug = slug[..MaxLength].TrimEnd('-');
        }

        return new Slug(slug);
    }

    /// <summary>
    /// Validates an existing canonical slug value.
    /// </summary>
    public static Slug Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Slug is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > MaxLength)
        {
            throw new DomainException($"Slug must not exceed {MaxLength} characters.");
        }

        if (!CanonicalPattern.IsMatch(trimmed))
        {
            throw new DomainException(
                "Slug must be canonical: lowercase letters and digits separated by single hyphens, with no leading or trailing hyphens.");
        }

        return new Slug(trimmed);
    }

    public override string ToString() => Value;
}