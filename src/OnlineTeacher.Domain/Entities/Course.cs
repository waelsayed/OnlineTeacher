using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A Course within a Teacher Platform tenant. A course is composed of an ordered set of Units.
/// The course carries a minimal lifecycle (Draft/Published). Duplicate course titles within a
/// platform are allowed; the title is descriptive, not an identity, and there is no public slug.
/// Ordering of units is an explicit, 1-based, contiguous integer sequence.
/// </summary>
public sealed class Course : IAuditable, ITenantScoped
{
    private readonly List<Unit> _units = [];

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string? Summary { get; private set; }

    public CourseStatus Status { get; private set; }

    /// <summary>Explicit commercial state: Free (no purchase) or Paid (requires a wallet purchase).</summary>
    public CoursePricingType PricingType { get; private set; }

    /// <summary>Price in EGP for Paid courses; null for Free courses.</summary>
    public decimal? Price { get; private set; }

    public IReadOnlyList<Unit> Units => _units;

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Course()
    {
    }

    public Course(Guid tenantId, string title, string? summary = null, CoursePricingType pricingType = CoursePricingType.Free, decimal? price = null)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("A course must belong to a tenant.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Course title is required.");
        }

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Title = title.Trim();
        Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        Status = CourseStatus.Draft;
        PricingType = pricingType;
        Price = price;
        ValidatePricing();
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Sets the course commercial state and price, validating that a Paid course has a positive price.</summary>
    public void SetPricing(CoursePricingType pricingType, decimal? price = null)
    {
        PricingType = pricingType;
        Price = price;
        ValidatePricing();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>True when the course is Paid (requires a wallet purchase).</summary>
    public bool IsPaid => PricingType == CoursePricingType.Paid;

    private void ValidatePricing()
    {
        if (PricingType == CoursePricingType.Paid && (Price is null || Price.Value <= 0m))
        {
            throw new DomainException("A paid course requires a positive price.");
        }

        if (PricingType == CoursePricingType.Free)
        {
            Price = null;
        }
    }

    /// <summary>Renames the course and updates the summary. Passing null keeps the existing value.</summary>
    public void Update(string? title, string? summary)
    {
        if (title is not null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("Course title is required.");
            }

            Title = title.Trim();
        }

        if (summary is not null)
        {
            Summary = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();
        }

        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Transitions the course to Published. Only a Draft course may be published.</summary>
    public void Publish()
    {
        if (Status != CourseStatus.Draft)
        {
            throw new DomainException($"Only a draft course can be published. Current status is '{Status}'.");
        }

        Status = CourseStatus.Published;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Returns the course to Draft. Only a Published course may be drafted.</summary>
    public void ToDraft()
    {
        if (Status != CourseStatus.Published)
        {
            throw new DomainException($"Only a published course can be returned to draft. Current status is '{Status}'.");
        }

        Status = CourseStatus.Draft;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Adds a unit at a specific 1-based position, shifting later units up to keep positions unique.</summary>
    public Unit AddUnit(string title, int position)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Unit title is required.");
        }

        if (position < 1)
        {
            throw new DomainException("Unit position must be a positive number.");
        }

        foreach (var existing in _units.Where(u => u.Position >= position).ToList())
        {
            existing.MoveToPosition(existing.Position + 1);
        }

        var unit = new Unit(Id, TenantId, title.Trim(), position);
        _units.Add(unit);
        SortUnits();
        UpdatedAtUtc = DateTime.UtcNow;
        return unit;
    }

    /// <summary>Appends a unit at the end of the course's ordering.</summary>
    public Unit AddUnit(string title)
    {
        var position = _units.Count == 0 ? 1 : _units.Max(u => u.Position) + 1;
        return AddUnit(title, position);
    }

    /// <summary>Removes a unit and re-indexes the remaining positions to stay contiguous.</summary>
    public void RemoveUnit(Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (unit.CourseId != Id)
        {
            throw new DomainException("Unit must belong to this course.");
        }

        if (!_units.Remove(unit))
        {
            return;
        }

        ReindexUnits();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Moves an existing unit to a new position, keeping positions unique and contiguous.</summary>
    public void MoveUnit(Unit unit, int newPosition)
    {
        ArgumentNullException.ThrowIfNull(unit);

        if (unit.CourseId != Id)
        {
            throw new DomainException("Unit must belong to this course.");
        }

        if (newPosition < 1 || newPosition > _units.Count)
        {
            throw new DomainException($"Unit position must be between 1 and {_units.Count}.");
        }

        if (!_units.Remove(unit))
        {
            return;
        }

        _units.Insert(newPosition - 1, unit);
        ReindexUnits();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ReindexUnits()
    {
        for (var i = 0; i < _units.Count; i++)
        {
            _units[i].MoveToPosition(i + 1);
        }
    }

    private void SortUnits()
    {
        _units.Sort((a, b) => a.Position.CompareTo(b.Position));
    }
}