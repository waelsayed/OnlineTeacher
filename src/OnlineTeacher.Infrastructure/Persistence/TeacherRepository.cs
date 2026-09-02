using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for the central Teacher aggregate.
/// </summary>
public sealed class TeacherRepository : ITeacherRepository
{
    private readonly ApplicationDbContext _db;

    public TeacherRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Teacher?> GetByIdAsync(Guid teacherId, CancellationToken cancellationToken = default) =>
        _db.Teachers.FirstOrDefaultAsync(t => t.Id == teacherId, cancellationToken);

    public Task<Teacher?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        _db.Teachers.FirstOrDefaultAsync(t => t.Email == email, cancellationToken);

    public void Add(Teacher teacher)
    {
        _db.Teachers.Add(teacher);
    }

    public void AddMembership(TeacherPlatformMembership membership)
    {
        _db.Memberships.Add(membership);
    }
}