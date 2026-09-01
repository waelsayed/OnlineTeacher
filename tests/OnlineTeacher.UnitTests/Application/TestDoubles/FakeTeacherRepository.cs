using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeTeacherRepository : ITeacherRepository
{
    private readonly List<Teacher> _teachers = [];

    public IReadOnlyList<Teacher> Teachers => _teachers;

    public void Seed(Teacher teacher)
    {
        _teachers.Add(teacher);
    }

    public Task<Teacher?> GetByIdAsync(Guid teacherId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_teachers.FirstOrDefault(t => t.Id == teacherId));

    public Task<Teacher?> GetByEmailAsync(OnlineTeacher.Domain.ValueObjects.Email email, CancellationToken cancellationToken = default) =>
        Task.FromResult(_teachers.FirstOrDefault(t => t.Email == email));

    public void Add(Teacher teacher)
    {
        _teachers.Add(teacher);
    }
}