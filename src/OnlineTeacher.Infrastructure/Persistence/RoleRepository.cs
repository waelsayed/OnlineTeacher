using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core data access for tenant-scoped roles.
/// </summary>
public sealed class RoleRepository : IRoleRepository
{
    private readonly ApplicationDbContext _db;

    public RoleRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default) =>
        _db.Roles.FirstOrDefaultAsync(r => r.TenantId == tenantId && r.Name == name, cancellationToken);

    public void Add(Role role)
    {
        _db.Roles.Add(role);
    }
}