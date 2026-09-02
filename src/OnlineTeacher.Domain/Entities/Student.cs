using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A student user with one central identity. A single student can follow multiple
/// Teacher Platforms using the same central account; the student identity is NOT
/// tenant-scoped and does not belong to a Teacher Platform.
/// </summary>
public sealed class Student : IAuditable
{
    private readonly List<StudentFollow> _follows = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Email Email { get; private set; } = null!;

    /// <summary>Stores the password hash. Never store or log plaintext passwords.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public IReadOnlyList<StudentFollow> Follows => _follows;

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Student()
    {
    }

    public Student(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Student name is required.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        Email = email ?? throw new ArgumentNullException(nameof(email));
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new DomainException("Password hash is required.");
        }

        PasswordHash = passwordHash;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void AddFollow(StudentFollow follow)
    {
        ArgumentNullException.ThrowIfNull(follow);

        if (follow.StudentId != Id)
        {
            throw new DomainException("Follow must belong to this student.");
        }

        if (_follows.Any(f => f.TeacherId == follow.TeacherId))
        {
            throw new DomainException("Student already follows this teacher.");
        }

        _follows.Add(follow);
    }

    public void RemoveFollow(StudentFollow follow)
    {
        ArgumentNullException.ThrowIfNull(follow);

        if (follow.StudentId != Id)
        {
            throw new DomainException("Follow must belong to this student.");
        }

        _follows.Remove(follow);
    }
}