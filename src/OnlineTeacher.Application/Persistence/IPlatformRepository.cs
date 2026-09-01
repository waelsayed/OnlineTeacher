using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for Teacher Platforms keyed by their stable public identity.
/// </summary>
public interface IPlatformRepository
{
    Task<TeacherPlatform?> GetByPublicIdAsync(PublicId publicId, CancellationToken cancellationToken = default);

    void Add(TeacherPlatform platform);
}