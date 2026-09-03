namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// A student enrolled in a Teacher Platform course, as presented to the platform member.
/// </summary>
public sealed record CourseEnrolledStudentResponse(
    Guid StudentId,
    string StudentName,
    DateTime EnrolledAtUtc)
{
    public static CourseEnrolledStudentResponse From(OnlineTeacher.Application.Persistence.EnrollmentStudentResponse item) =>
        new(item.StudentId, item.StudentName, item.EnrolledAtUtc);
}
