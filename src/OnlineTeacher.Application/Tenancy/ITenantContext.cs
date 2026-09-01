namespace OnlineTeacher.Application.Tenancy;

/// <summary>
/// Resolves the current Teacher Platform (tenant) for the active request/scope.
/// Tenant resolution precedes authorization and tenant-aware data access.
/// </summary>
public interface ITenantContext
{
    Guid? TenantId { get; }

    /// <summary>
    /// Sets the current tenant. Returns false when the scope already holds a different tenant.
    /// </summary>
    bool TrySetTenant(Guid tenantId);

    void Clear();
}