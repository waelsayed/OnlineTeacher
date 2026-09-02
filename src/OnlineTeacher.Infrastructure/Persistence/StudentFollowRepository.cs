using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for the central, non-tenant-scoped Student ↔ Teacher follow
/// relationship. Duplicate (Student, Teacher) pairs are rejected by a database unique constraint.
/// </summary>
public sealed class StudentFollowRepository : IStudentFollowRepository
{
    private readonly ApplicationDbContext _db;

    public StudentFollowRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<StudentFollow?> GetAsync(Guid studentId, Guid teacherId, CancellationToken cancellationToken = default) =>
        _db.StudentFollows.FirstOrDefaultAsync(
            f => f.StudentId == studentId && f.TeacherId == teacherId,
            cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListTeacherIdsAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        await _db.StudentFollows
            .Where(f => f.StudentId == studentId)
            .OrderBy(f => f.CreatedAtUtc)
            .Select(f => f.TeacherId)
            .ToListAsync(cancellationToken);

    public void Add(StudentFollow follow)
    {
        _db.StudentFollows.Add(follow);
    }

    public void Remove(StudentFollow follow)
    {
        _db.StudentFollows.Remove(follow);
    }
}