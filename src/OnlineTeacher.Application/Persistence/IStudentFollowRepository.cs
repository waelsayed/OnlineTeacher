using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for the central, non-tenant-scoped Student ↔ Teacher follow relationship.
/// Duplicate (Student, Teacher) pairs are prevented by a database unique constraint.
/// </summary>
public interface IStudentFollowRepository
{
    Task<StudentFollow?> GetAsync(Guid studentId, Guid teacherId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> ListTeacherIdsAsync(Guid studentId, CancellationToken cancellationToken = default);

    void Add(StudentFollow follow);

    void Remove(StudentFollow follow);
}