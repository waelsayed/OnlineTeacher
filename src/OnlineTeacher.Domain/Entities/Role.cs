using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// A role within a Teacher Platform tenant, composed of permissions.
/// Role names are unique within a tenant. Avoid creating dozens of specialized roles.
/// </summary>
public sealed class Role : IAuditable, ITenantScoped
{
    private readonly List<RolePermission> _permissions = [];

    public Guid Id { get; private set; }

    public Guid TenantId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public IReadOnlyList<RolePermission> Permissions => _permissions;

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? CreatedBy { get; private set; }

    public DateTime? UpdatedAtUtc { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    private Role()
    {
    }

    public Role(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
        {
            throw new DomainException("A role must belong to a tenant.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Role name is required.");
        }

        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name.Trim();
        CreatedAtUtc = DateTime.UtcNow;
    }

    public RolePermission AddPermission(Permission permission)
    {
        ArgumentNullException.ThrowIfNull(permission);

        if (_permissions.Any(rp => rp.PermissionId == permission.Id))
        {
            throw new DomainException($"Permission '{permission.Code}' is already assigned to role '{Name}'.");
        }

        var rolePermission = new RolePermission(Id, permission.Id, TenantId);
        _permissions.Add(rolePermission);
        return rolePermission;
    }
}