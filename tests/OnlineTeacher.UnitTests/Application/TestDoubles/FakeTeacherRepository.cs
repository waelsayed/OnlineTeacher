using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeTeacherRepository : ITeacherRepository
{
    private readonly List<Teacher> _teachers = [];
    private readonly List<TeacherPlatformMembership> _memberships = [];

    public IReadOnlyList<Teacher> Teachers => _teachers;

    public IReadOnlyList<TeacherPlatformMembership> Memberships => _memberships;

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

    public void AddMembership(TeacherPlatformMembership membership)
    {
        _memberships.Add(membership);
    }
}