using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Reads a teacher's access profile within one tenant. The tenant-scoped entity sets
/// (memberships, roles, role permissions) are automatically filtered to the active
/// tenant context; the request must therefore run with the tenant established.
/// </summary>
public sealed class TeacherPlatformAccessRepository : ITeacherPlatformAccessRepository
{
    private readonly ApplicationDbContext _db;

    public TeacherPlatformAccessRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TeacherPlatformAccess?> GetAsync(Guid teacherId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var platform = await _db.TeacherPlatforms.FirstOrDefaultAsync(p => p.Id == tenantId, cancellationToken);
        if (platform is null)
        {
            return null;
        }

        var membership = await _db.Memberships.FirstOrDefaultAsync(
            m => m.TeacherId == teacherId && m.TeacherPlatformId == tenantId,
            cancellationToken);
        if (membership is null)
        {
            return null;
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == membership.RoleId, cancellationToken);
        if (role is null)
        {
            return null;
        }

        var permissionCodes = await _db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .Join(_db.Permissions, rp => rp.PermissionId, p => p.Id, (rp, p) => p.Code)
            .OrderBy(code => code)
            .ToListAsync(cancellationToken);

        return new TeacherPlatformAccess(
            teacherId,
            platform.Id,
            platform.PublicId.Value,
            platform.Slug.Value,
            platform.Status,
            membership.IsOwner,
            new[] { role.Name },
            permissionCodes);
    }
}