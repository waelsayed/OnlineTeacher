using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for the central Student aggregate. Students are central and NOT
/// tenant-scoped, so no tenant query filter applies.
/// </summary>
public sealed class StudentRepository : IStudentRepository
{
    private readonly ApplicationDbContext _db;

    public StudentRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Student?> GetByIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.Id == studentId, cancellationToken);

    public Task<Student?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        _db.Students.FirstOrDefaultAsync(s => s.Email == email, cancellationToken);

    public void Add(Student student)
    {
        _db.Students.Add(student);
    }
}