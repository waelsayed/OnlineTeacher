namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// Lifecycle state of a Teacher Platform.
/// </summary>
public enum PlatformStatus
{
    /// <summary>Created but not yet activated.</summary>
    PendingActivation = 0,

    /// <summary>Active and usable.</summary>
    Active = 1,

    /// <summary>Deactivated and no longer active.</summary>
    Deactivated = 2
}