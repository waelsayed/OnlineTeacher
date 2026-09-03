using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// A student's enrollment in a Teacher Platform course, as presented to the student. Mapped
/// from <see cref="EnrollmentListItem"/>. Exposes the enrollment and course public identity; the
/// internal student identity is intentionally not part of this projection.
/// </summary>
public sealed record EnrollmentListItemResponse(
    Guid EnrollmentId,
    Guid CourseId,
    string CourseTitle,
    string Status,
    DateTime EnrolledAtUtc)
{
    public static EnrollmentListItemResponse From(EnrollmentListItem item) =>
        new(item.EnrollmentId, item.CourseId, item.CourseTitle, item.Status, item.EnrolledAtUtc);
}
