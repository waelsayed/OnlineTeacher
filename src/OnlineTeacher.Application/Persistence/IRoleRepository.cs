using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for tenant-scoped roles.
/// </summary>
public interface IRoleRepository
{
    /// <summary>Returns a role by name within the given tenant, or null when absent.</summary>
    Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default);

    void Add(Role role);
}