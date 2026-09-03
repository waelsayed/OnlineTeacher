using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for the Course aggregate. Courses and their nested Units/Lessons are
/// tenant-scoped and subject to the tenant query filter; reads are additionally filtered by the
/// explicit tenant id to make a cross-tenant course-id lookup return null rather than foreign data.
/// Since Units and Lessons are cascade children, deleting or saving a Course also persists them.
/// </summary>
public sealed class CourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _db;

    public CourseRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Course?> GetByIdAsync(Guid tenantId, Guid courseId, CancellationToken cancellationToken = default) =>
        _db.Courses
            .Include(c => c.Units.OrderBy(u => u.Position))
                .ThenInclude(u => u.Lessons.OrderBy(l => l.Position))
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.Id == courseId,
                cancellationToken);

    public async Task<IReadOnlyList<Course>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        await _db.Courses
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);

    public void Add(Course course)
    {
        _db.Courses.Add(course);
    }

    public void Remove(Course course)
    {
        _db.Courses.Remove(course);
    }

    public void AddUnit(Unit unit)
    {
        _db.Units.Add(unit);
    }

    public void AddLesson(Lesson lesson)
    {
        _db.Lessons.Add(lesson);
    }
}