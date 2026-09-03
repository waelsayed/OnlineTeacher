using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A Unit within a Course (and thus within a Teacher Platform tenant). A unit groups an ordered
/// set of Lessons. Unit positions are unique within a course. The course and tenant are snapshot
/// directly so a unit is never orphaned outside its owning course's tenant.
/// </summary>
public sealed class Unit : IAuditable, ITenantScoped
{
    private readonly List<Lesson> _lessons = [];

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public Guid CourseId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public int Position { get; private set; }

    public IReadOnlyList<Lesson> Lessons => _lessons;

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Unit()
    {
    }

    public Unit(Guid courseId, Guid tenantId, string title, int position)
    {
        if (courseId == Guid.Empty)
        {
            throw new DomainException("A unit must belong to a course.");
        }

        if (tenantId == Guid.Empty)
        {
            throw new DomainException("A unit must belong to a tenant.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new DomainException("Unit title is required.");
        }

        if (position < 1)
        {
            throw new DomainException("Unit position must be a positive number.");
        }

        Id = Guid.NewGuid();
        CourseId = courseId;
        TenantId = tenantId;
        Title = title.Trim();
        Position = position;
        CreatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Renames the unit. Passing null keeps the existing title.</summary>
    public void Rename(string? title)
    {
        if (title is not null)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                throw new DomainException("Unit title is required.");
            }

            Title = title.Trim();
            UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>Adds a lesson at a specific 1-based position, shifting later lessons up to keep positions unique.</summary>
    public Lesson AddLesson(string lessonTitle, int position)
    {
        if (string.IsNullOrWhiteSpace(lessonTitle))
        {
            throw new DomainException("Lesson title is required.");
        }

        if (position < 1)
        {
            throw new DomainException("Lesson position must be a positive number.");
        }

        foreach (var existing in _lessons.Where(l => l.Position >= position).ToList())
        {
            existing.MoveToPosition(existing.Position + 1);
        }

        var lesson = new Lesson(Id, CourseId, TenantId, lessonTitle.Trim(), position);
        _lessons.Add(lesson);
        SortLessons();
        UpdatedAtUtc = DateTime.UtcNow;
        return lesson;
    }

    /// <summary>Appends a lesson at the end of the unit's ordering.</summary>
    public Lesson AddLesson(string lessonTitle)
    {
        var position = _lessons.Count == 0 ? 1 : _lessons.Max(l => l.Position) + 1;
        return AddLesson(lessonTitle, position);
    }

    /// <summary>Removes a lesson and re-indexes the remaining positions to stay contiguous.</summary>
    public void RemoveLesson(Lesson lesson)
    {
        ArgumentNullException.ThrowIfNull(lesson);

        if (lesson.UnitId != Id)
        {
            throw new DomainException("Lesson must belong to this unit.");
        }

        if (!_lessons.Remove(lesson))
        {
            return;
        }

        ReindexLessons();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    /// <summary>Moves an existing lesson to a new position, keeping positions unique and contiguous.</summary>
    public void MoveLesson(Lesson lesson, int newPosition)
    {
        ArgumentNullException.ThrowIfNull(lesson);

        if (lesson.UnitId != Id)
        {
            throw new DomainException("Lesson must belong to this unit.");
        }

        if (newPosition < 1 || newPosition > _lessons.Count)
        {
            throw new DomainException($"Lesson position must be between 1 and {_lessons.Count}.");
        }

        if (!_lessons.Remove(lesson))
        {
            return;
        }

        _lessons.Insert(newPosition - 1, lesson);
        ReindexLessons();
        UpdatedAtUtc = DateTime.UtcNow;
    }

    internal void MoveToPosition(int position)
    {
        Position = position;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void ReindexLessons()
    {
        for (var i = 0; i < _lessons.Count; i++)
        {
            _lessons[i].MoveToPosition(i + 1);
        }
    }

    private void SortLessons()
    {
        _lessons.Sort((a, b) => a.Position.CompareTo(b.Position));
    }
}