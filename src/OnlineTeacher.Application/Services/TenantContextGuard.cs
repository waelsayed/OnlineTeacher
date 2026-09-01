using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Tenancy;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Guards central operations against accidentally running under a teacher tenant context.
/// Central operations must not execute inside a tenant scope.
/// </summary>
internal static class TenantContextGuard
{
    public static void EnsureCentral(ITenantContext tenantContext)
    {
        if (tenantContext.TenantId.HasValue)
        {
            throw new TenantMismatchException("A central operation cannot run under a teacher tenant context.");
        }
    }
}