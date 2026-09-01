using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles = [];

    public IReadOnlyList<Role> Roles => _roles;

    public void Add(Role role)
    {
        _roles.Add(role);
    }
}