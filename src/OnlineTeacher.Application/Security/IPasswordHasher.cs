namespace OnlineTeacher.Application.Security;

/// <summary>
/// Password hashing port. Uses the framework-provided PasswordHasher internally.
/// Never stores or logs plaintext passwords.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string storedHash);
}