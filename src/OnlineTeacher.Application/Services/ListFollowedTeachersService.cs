using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Lists the teachers a student follows, presented as their browseable public platform(s).
/// This is a central read that never exposes internal database identifiers.
/// </summary>
public sealed class ListFollowedTeachersService
{
    private readonly IStudentRepository _students;
    private readonly IStudentFollowRepository _follows;
    private readonly IPlatformMembershipRepository _memberships;

    public ListFollowedTeachersService(
        IStudentRepository students,
        IStudentFollowRepository follows,
        IPlatformMembershipRepository memberships)
    {
        _students = students;
        _follows = follows;
        _memberships = memberships;
    }

    public async Task<IReadOnlyList<FollowedTeacher>> ListAsync(Guid studentId, CancellationToken cancellationToken = default)
    {
        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var teacherIds = await _follows.ListTeacherIdsAsync(studentId, cancellationToken);

        var result = new List<FollowedTeacher>();
        foreach (var teacherId in teacherIds)
        {
            var platforms = await _memberships.GetOwnedPlatformsAsync(teacherId, cancellationToken);
            foreach (var platform in platforms)
            {
                result.Add(new FollowedTeacher(platform.PublicId, platform.Slug));
            }
        }

        return result;
    }
}