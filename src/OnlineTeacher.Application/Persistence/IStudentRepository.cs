using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for the central Student aggregate. Student identity is central and
/// NOT tenant-scoped, so this repository never applies a tenant filter.
/// </summary>
public interface IStudentRepository
{
    Task<Student?> GetByIdAsync(Guid studentId, CancellationToken cancellationToken = default);

    Task<Student?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);

    void Add(Student student);
}