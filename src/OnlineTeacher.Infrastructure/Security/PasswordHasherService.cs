using Microsoft.AspNetCore.Identity;
using OnlineTeacher.Application.Security;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Security;

/// <summary>
/// Framework-provided password hashing (PasswordHasher&lt;Teacher&gt;).
/// </summary>
public sealed class PasswordHasherService : IPasswordHasher
{
    private readonly PasswordHasher<Teacher> _hasher = new();

    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string password, string storedHash) =>
        _hasher.VerifyHashedPassword(null!, storedHash, password) != PasswordVerificationResult.Failed;
}