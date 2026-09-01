using System.Security.Cryptography;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.ValueObjects;

/// <summary>
/// Stable, non-sequential, cryptographically generated public identifier for a Teacher Platform.
/// Safe for use in public URLs and globally unique. Never expose sequential database IDs as public identifiers.
/// </summary>
public sealed record PublicId
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    private const int Length = 12;

    private static readonly HashSet<char> AllowedCharacters = new(Alphabet);

    public string Value { get; }

    private PublicId(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Generates a new cryptographically random public identifier.
    /// Uniqueness is protected by the database unique constraint plus the 
    /// </summary>
    public static PublicId Generate()
    {
        var bytes = RandomNumberGenerator.GetBytes(Length);
        var characters = new char[Length];

        for (var i = 0; i < Length; i++)
        {
            characters[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new PublicId(new string(characters));
    }

    public static PublicId Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException("Public id is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length != Length)
        {
            throw new DomainException($"Public id must be exactly {Length} characters.");
        }

        if (!trimmed.All(AllowedCharacters.Contains))
        {
            throw new DomainException($"Public id may only contain alphanumeric URL-safe characters.");
        }

        return new PublicId(trimmed);
    }

    public override string ToString() => Value;
}