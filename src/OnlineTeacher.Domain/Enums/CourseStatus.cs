namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// Lifecycle state of a Course. A draft is still being prepared; a published course is
/// available to the platform's audience. Enrollment may later treat Published as a
/// prerequisite, but that is outside this scope.
/// </summary>
public enum CourseStatus
{
    /// <summary>Content is still being prepared and is not yet considered published.</summary>
    Draft = 0,

    /// <summary>Content is published and available.</summary>
    Published = 1
}