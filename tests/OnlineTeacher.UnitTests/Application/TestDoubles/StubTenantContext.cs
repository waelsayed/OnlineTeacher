using OnlineTeacher.Application.Tenancy;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class StubTenantContext : ITenantContext
{
    public Guid? TenantId { get; private set; }

    public bool TrySetTenant(Guid tenantId)
    {
        if (TenantId.HasValue)
        {
            return false;
        }

        TenantId = tenantId;
        return true;
    }

    public void Clear() => TenantId = null;
}