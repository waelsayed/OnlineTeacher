using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A Teacher Platform. A tenant in the system.
/// Public identity is a stable, non-sequential PublicId; the Slug is a canonical URL component
/// and must never be treated as primary identity.
/// </summary>
public sealed class TeacherPlatform : IAuditable
{
    public Guid Id { get; private set; }

    /// <summary>Stable public identifier. Globally unique.</summary>
    public PublicId PublicId { get; private set; } = null!;

    /// <summary>Canonical URL slug. NOT globally unique.</summary>
    public Slug Slug { get; private set; } = null!;

    public string Name { get; private set; } = string.Empty;

    public PlatformStatus Status { get; private set; }

    public DateTime? ActivatedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private TeacherPlatform()
    {
    }

    public TeacherPlatform(string name, PublicId publicId, Slug slug)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Platform name is required.");
        }

        Id = Guid.NewGuid();
        Name = name.Trim();
        PublicId = publicId ?? throw new ArgumentNullException(nameof(publicId));
        Slug = slug ?? throw new ArgumentNullException(nameof(slug));
        Status = PlatformStatus.PendingActivation;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the platform from PendingActivation to Active.
    /// </summary>
    public void Activate()
    {
        if (Status != PlatformStatus.PendingActivation)
        {
            throw new DomainException(
                $"Cannot activate platform in '{Status}' status. Only a pending platform can be activated.");
        }

        Status = PlatformStatus.Active;
        ActivatedAtUtc = DateTime.UtcNow;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Transitions the platform from Active to Deactivated.
    /// </summary>
    public void Deactivate()
    {
        if (Status != PlatformStatus.Active)
        {
            throw new DomainException("Only an active platform can be deactivated.");
        }

        Status = PlatformStatus.Deactivated;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Renames the platform. The internal Id and the stable PublicId are never changed;
    /// only the editable display name is updated.
    /// </summary>
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Platform name is required.");
        }

        Name = name.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Changes the canonical slug. Routing later resolves the current canonical slug for redirect purposes.
    /// </summary>
    public void ChangeSlug(Slug newSlug)
    {
        Slug = newSlug ?? throw new ArgumentNullException(nameof(newSlug));
        UpdatedAtUtc = DateTime.UtcNow;
    }
}