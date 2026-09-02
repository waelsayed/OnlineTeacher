using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for Teacher Platforms keyed by their stable public identity.
/// </summary>
public sealed class PlatformRepository : IPlatformRepository
{
    private readonly ApplicationDbContext _db;

    public PlatformRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<TeacherPlatform?> GetByPublicIdAsync(PublicId publicId, CancellationToken cancellationToken = default) =>
        _db.TeacherPlatforms.FirstOrDefaultAsync(p => p.PublicId == publicId, cancellationToken);

    public void Add(TeacherPlatform platform)
    {
        _db.TeacherPlatforms.Add(platform);
    }
}