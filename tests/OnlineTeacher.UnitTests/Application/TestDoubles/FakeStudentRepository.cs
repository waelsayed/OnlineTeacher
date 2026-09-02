using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeStudentRepository : IStudentRepository
{
    private readonly List<Student> _students = [];

    public IReadOnlyList<Student> Students => _students;

    public void Seed(Student student)
    {
        _students.Add(student);
    }

    public Task<Student?> GetByIdAsync(Guid studentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_students.FirstOrDefault(s => s.Id == studentId));

    public Task<Student?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_students.FirstOrDefault(s => s.Email == email));

    public void Add(Student student)
    {
        _students.Add(student);
    }
}