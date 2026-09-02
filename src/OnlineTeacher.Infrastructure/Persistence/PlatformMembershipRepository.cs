using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Permissions;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for tenant-scoped memberships. Reads run under the active tenant
/// context so the tenant query filter keeps a platform's members isolated from others.
/// </summary>
public sealed class PlatformMembershipRepository : IPlatformMembershipRepository
{
    private readonly ApplicationDbContext _db;

    public PlatformMembershipRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PlatformMember>> GetMembersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var memberships = await _db.Memberships
            .Where(m => m.TeacherPlatformId == tenantId)
            .ToListAsync(cancellationToken);

        if (memberships.Count == 0)
        {
            return Array.Empty<PlatformMember>();
        }

        var teacherIds = memberships.Select(m => m.TeacherId).ToArray();
        var roleIds = memberships.Select(m => m.RoleId).ToArray();

        var teachers = await _db.Teachers
            .Where(t => teacherIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => t.Name, cancellationToken);

        var roles = await _db.Roles
            .Where(r => roleIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Name, cancellationToken);

        return memberships
            .OrderByDescending(m => m.IsOwner)
            .ThenBy(m => teachers.GetValueOrDefault(m.TeacherId, m.TeacherId.ToString()))
            .Select(m => new PlatformMember(
                m.TeacherId,
                teachers.GetValueOrDefault(m.TeacherId, m.TeacherId.ToString()),
                m.RoleId,
                roles.GetValueOrDefault(m.RoleId, PlatformRoles.Assistant),
                m.IsOwner))
            .ToList();
    }

    public Task<TeacherPlatformMembership?> GetForTeacherAsync(Guid tenantId, Guid teacherId, CancellationToken cancellationToken = default) =>
        _db.Memberships.FirstOrDefaultAsync(
            m => m.TeacherPlatformId == tenantId && m.TeacherId == teacherId,
            cancellationToken);

    public void Remove(TeacherPlatformMembership membership)
    {
        _db.Memberships.Remove(membership);
    }
}