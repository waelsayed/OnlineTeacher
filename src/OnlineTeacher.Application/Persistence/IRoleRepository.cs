using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Data access for tenant-scoped roles.
/// </summary>
public interface IRoleRepository
{
    void Add(Role role);
}