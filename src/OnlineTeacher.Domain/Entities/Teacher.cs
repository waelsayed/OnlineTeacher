using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A teacher user with one central identity (Central Platform concern).
/// A single teacher can hold memberships across multiple Teacher Platforms.
/// </summary>
public sealed class Teacher : IAuditable
{
    private readonly List<TeacherPlatformMembership> _memberships = [];

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Email Email { get; private set; } = null!;

    /// <summary>Stores the password hash. Never store or log plaintext passwords.</summary>
    public string PasswordHash { get; private set; } = string.Empty;

    public IReadOnlyList<TeacherPlatformMembership> Memberships => _memberships;

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Teacher()
    {
    }

    public Teacher(string name, Email email)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Teacher name is required.");
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

    public void AddMembership(TeacherPlatformMembership membership)
    {
        ArgumentNullException.ThrowIfNull(membership);

        if (membership.TeacherId != Id)
        {
            throw new DomainException("Membership must belong to this teacher.");
        }

        if (_memberships.Any(m => m.TenantId == membership.TenantId))
        {
            throw new DomainException("Teacher already has a membership in this platform.");
        }

        _memberships.Add(membership);
    }
}