namespace OnlineTeacher.Domain.Enums;

/// <summary>
/// Lifecycle state of a Student Enrollment in a Course.
/// Active means the student is currently enrolled.
/// Cancelled means the student has withdrawn from the course (terminal state).
/// </summary>
public enum EnrollmentStatus
{
    /// <summary>The student is actively enrolled in the course.</summary>
    Active = 0,

    /// <summary>The student has cancelled their enrollment (terminal state).</summary>
    Cancelled = 1
}
