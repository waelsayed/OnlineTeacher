using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeStudentFollowRepository : IStudentFollowRepository
{
    private readonly List<StudentFollow> _follows = [];

    public IReadOnlyList<StudentFollow> Follows => _follows;

    public void Seed(StudentFollow follow)
    {
        _follows.Add(follow);
    }

    public Task<StudentFollow?> GetAsync(Guid studentId, Guid teacherId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_follows.FirstOrDefault(f => f.StudentId == studentId && f.TeacherId == teacherId));

    public Task<IReadOnlyList<Guid>> ListTeacherIdsAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> ids = _follows.Where(f => f.StudentId == studentId).Select(f => f.TeacherId).ToList();
        return Task.FromResult(ids);
    }

    public void Add(StudentFollow follow)
    {
        _follows.Add(follow);
    }

    public void Remove(StudentFollow follow)
    {
        _follows.Remove(follow);
    }
}