using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for the central Teacher aggregate.
/// </summary>
public interface ITeacherRepository
{
    Task<Teacher?> GetByIdAsync(Guid teacherId, CancellationToken cancellationToken = default);

    Task<Teacher?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    void Add(Teacher teacher);
}