using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Checks whether the current student follows the teacher behind a Teacher Platform publicId.
/// Returns true only when a central (Student, Teacher) follow relationship exists.
/// </summary>
public sealed class IsFollowingTeacherService
{
    private readonly IStudentRepository _students;
    private readonly IPlatformRepository _platforms;
    private readonly IPlatformMembershipRepository _memberships;
    private readonly IStudentFollowRepository _follows;
    private readonly ITenantContext _tenantContext;

    public IsFollowingTeacherService(
        IStudentRepository students,
        IPlatformRepository platforms,
        IPlatformMembershipRepository memberships,
        IStudentFollowRepository follows,
        ITenantContext tenantContext)
    {
        _students = students;
        _platforms = platforms;
        _memberships = memberships;
        _follows = follows;
        _tenantContext = tenantContext;
    }

    public async Task<bool> IsFollowingAsync(
        Guid studentId,
        string? teacherPublicId,
        CancellationToken cancellationToken = default)
    {
        var student = await _students.GetByIdAsync(studentId, cancellationToken);
        if (student is null)
        {
            return false;
        }

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);
        var teacherId = await ResolveOwnerTeacherIdAsync(platform.Id, cancellationToken);

        return await _follows.GetAsync(studentId, teacherId, cancellationToken) is not null;
    }

    private async Task<Guid> ResolveOwnerTeacherIdAsync(Guid platformId, CancellationToken cancellationToken)
    {
        var currentTenant = _tenantContext.TenantId;

        try
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.TrySetTenant(platformId);
            }

            return await _memberships.GetOwnerTeacherIdAsync(platformId, cancellationToken)
                ?? throw new Exceptions.NotFoundException("The teacher platform has no owner.");
        }
        finally
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.Clear();
            }
        }
    }
}