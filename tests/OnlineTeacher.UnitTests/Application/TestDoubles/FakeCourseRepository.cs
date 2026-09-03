using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeCourseRepository : ICourseRepository
{
    private readonly List<Course> _courses = [];

    public IReadOnlyList<Course> Courses => _courses;

    public void Seed(Course course)
    {
        _courses.Add(course);
    }

    public Task<Course?> GetByIdAsync(Guid tenantId, Guid courseId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_courses.FirstOrDefault(c => c.TenantId == tenantId && c.Id == courseId));

    public Task<IReadOnlyList<Course>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Course> result = _courses
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Title)
            .ToList();
        return Task.FromResult(result);
    }

    public void Add(Course course)
    {
        _courses.Add(course);
    }

    public void Remove(Course course)
    {
        _courses.Remove(course);
    }

    public void AddUnit(Unit unit)
    {
        // No-op: the domain aggregate already places the unit inside its course collection.
    }

    public void AddLesson(Lesson lesson)
    {
        // No-op: the domain aggregate already places the lesson inside its unit collection.
    }
}