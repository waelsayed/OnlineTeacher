using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles = [];

    public IReadOnlyList<Role> Roles => _roles;

    public void Seed(Role role)
    {
        _roles.Add(role);
    }

    public Task<Role?> GetByNameAsync(Guid tenantId, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(_roles.FirstOrDefault(r => r.TenantId == tenantId && r.Name == name));

    public void Add(Role role)
    {
        _roles.Add(role);
    }
}