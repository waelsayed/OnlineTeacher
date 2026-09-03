using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for tenant-scoped Course aggregates. All reads are explicitly scoped by the
/// resolved tenant id so a caller-supplied course id can never return a course from another
/// tenant; the EF tenant query filter acts as a further defense-in-depth guard.
/// </summary>
public interface ICourseRepository
{
    /// <summary>Loads a course (with its units and lessons) belonging to the given tenant.</summary>
    Task<Course?> GetByIdAsync(Guid tenantId, Guid courseId, CancellationToken cancellationToken = default);

    /// <summary>Lists the courses belonging to the given tenant.</summary>
    Task<IReadOnlyList<Course>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);

    void Add(Course course);

    void Remove(Course course);

    /// <summary>
    /// Registers a newly-created Unit as an "Added" entity. Explicitly marking a new aggregate
    /// child as Added avoids EF relationship-fixup heuristics that can otherwise persist a brand-new
    /// child as a Modified row, which would fail (0 rows affected) because the row does not exist yet.
    /// </summary>
    void AddUnit(Unit unit);

    /// <summary>Registers a newly-created Lesson as an "Added" entity. See <see cref="AddUnit"/>.</summary>
    void AddLesson(Lesson lesson);
}