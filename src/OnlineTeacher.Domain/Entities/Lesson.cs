using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A Lesson within a Unit (and thus within a Course and a Teacher Platform tenant). Lessons carry
/// an explicit position unique within the unit. No media/file content is modelled here.
/// </summary>
public sealed class Lesson : IAuditable, ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid UnitId { get; private set; }

    public Guid CourseId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Lesson()
    {
    }

    public Lesson(Guid unitId, Guid courseId, Guid tenantId, string title, int position)
    {
        if (unitId == Guid.Empty)
        {
            throw new DomainException("A lesson must belong to a unit.");
        }

        if (courseId == Guid.Empty)
        {
            throw new DomainException("A lesson must belong to a course.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new DomainException("A lesson must belong to a tenant.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Lesson title is required.");
        }

        if (position < 1)
        {
            throw new DomainException("Lesson position must be a positive number.");
        }

        Id = Guid.NewGuid();
        UnitId = unitId;
        CourseId = courseId;
        TenantId = tenantId;
        Title = title.Trim();
        Position = position;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Renames the lesson. Passing null keeps the existing title.</summary>
    public void Rename(string? title)
    {
        if (title is not null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("Lesson title is required.");
            }

            Title = title.Trim();
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    internal void MoveToPosition(int position)
    {
        Position = position;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}