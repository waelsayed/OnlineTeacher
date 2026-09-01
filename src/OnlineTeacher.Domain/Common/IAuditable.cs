namespace OnlineTeacher.Domain.Common;

/// <summary>
/// Provides audit information for domain entities.
/// Audit records are historical and must not be silently modified or deleted.
/// </summary>
public interface IAuditable
{
    DateTime CreatedAtUtc { get; }
    Guid? CreatedBy { get; }
    DateTime? UpdatedAtUtc { get; }
    Guid? UpdatedBy { get; }
}