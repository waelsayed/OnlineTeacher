using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// A single member of the resolved Teacher Platform, mapped from
/// <see cref="PlatformMember"/> for the API boundary.
/// </summary>
public sealed record PlatformMemberResponse(
    Guid TeacherId,
    string TeacherName,
    string RoleName,
    bool IsOwner)
{
    public static PlatformMemberResponse From(PlatformMember member) =>
        new(member.TeacherId, member.TeacherName, member.RoleName, member.IsOwner);
}