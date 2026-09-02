using Microsoft.EntityFrameworkCore;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Permissions;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Seeds the global permission catalog (Platform.Access, Platform.Manage).
/// Deterministic and idempotent: any existing permission is left untouched and only
/// missing codes are inserted. Runs once at startup so platform creation can always
/// resolve the required permissions from the catalog.
/// </summary>
public sealed class PermissionSeeder
{
    private readonly ApplicationDbContext _db;

    public PermissionSeeder(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existing = await _db.Permissions
            .Where(p => PlatformPermissions.All.Contains(p.Code))
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);

        var missing = PlatformPermissions.All.Except(existing, StringComparer.Ordinal).ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        foreach (var code in missing)
        {
            _db.Permissions.Add(new Permission(code));
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}