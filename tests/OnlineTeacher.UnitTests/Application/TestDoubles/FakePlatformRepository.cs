using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakePlatformRepository : IPlatformRepository
{
    private readonly List<TeacherPlatform> _platforms = [];

    public IReadOnlyList<TeacherPlatform> Platforms => _platforms;

    public void Seed(TeacherPlatform platform)
    {
        _platforms.Add(platform);
    }

    public Task<TeacherPlatform?> GetByPublicIdAsync(PublicId publicId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_platforms.FirstOrDefault(p => p.PublicId == publicId));

    public void Add(TeacherPlatform platform)
    {
        _platforms.Add(platform);
    }
}