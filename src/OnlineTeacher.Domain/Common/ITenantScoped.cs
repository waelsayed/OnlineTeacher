namespace OnlineTeacher.Domain.Common;

/// <summary>
/// Marks an entity as owned by a Teacher Platform (tenant).
/// Tenant-owned data must always be accessed within the correct tenant context.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }
}