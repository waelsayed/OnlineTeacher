using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Removes a central Student → Teacher follow relationship. The target is addressed by a
/// Teacher Platform publicId and resolves to its owner Teacher. Unfollowing a Teacher the
/// student does not follow is a safe no-op that never corrupts data.
/// </summary>
public sealed class UnfollowTeacherService
{
    private readonly IPlatformRepository _platforms;
    private readonly IPlatformMembershipRepository _memberships;
    private readonly IStudentRepository _students;
    private readonly IStudentFollowRepository _follows;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public UnfollowTeacherService(
        IPlatformRepository platforms,
        IPlatformMembershipRepository memberships,
        IStudentRepository students,
        IStudentFollowRepository follows,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _memberships = memberships;
        _students = students;
        _follows = follows;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task UnfollowAsync(
        Guid studentId,
        string? teacherPublicId,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);
        var teacherId = await ResolveOwnerTeacherIdAsync(platform.Id, cancellationToken);

        var follow = await _follows.GetAsync(studentId, teacherId, cancellationToken);
        if (follow is null)
        {
            return;
        }

        try
        {
            student.RemoveFollow(follow);
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        _follows.Remove(follow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
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
                ?? throw new NotFoundException("The teacher platform has no owner.");
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