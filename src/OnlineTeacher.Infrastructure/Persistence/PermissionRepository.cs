using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// EF Core read access to the global permission catalog.
/// </summary>
public sealed class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _db;

    public PermissionRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        _db.Permissions.FirstOrDefaultAsync(p => p.Code == code, cancellationToken);
}