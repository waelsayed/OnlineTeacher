using OnlineTeacher.Domain.Common;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Domain.Entities;

/// <summary>
/// Joins a Role to a Permission. Tenant-scoped.
/// A role must not have the same permission assigned twice within its tenant.
/// </summary>
public sealed class RolePermission : ITenantScoped
{
    public Guid Id { get; private set; }

    public Guid RoleId { get; private set; }

    public Guid PermissionId { get; private set; }

    public Guid TenantId { get; private set; }

    private RolePermission()
    {
    }

    internal RolePermission(Guid roleId, Guid permissionId, Guid tenantId)
    {
        Id = Guid.NewGuid();
        RoleId = roleId;
        PermissionId = permissionId;
        TenantId = tenantId;
    }
}