using OnlineTeacher.Application.Tenancy;

namespace OnlineTeacher.Infrastructure.Tenancy;

/// <summary>
/// Scoped tenant context. A scope may hold at most one tenant; attempts to switch
/// tenants mid-scope are rejected to protect tenant isolation.
/// </summary>
public sealed class TenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool TrySetTenant(Guid tenantId)
    {
        if (TenantId is not null && TenantId != tenantId)
        {
            return false;
        }

        TenantId = tenantId;
        return true;
    }

    public void Clear() => TenantId = null;
}