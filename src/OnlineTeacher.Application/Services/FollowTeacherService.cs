using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Registers a central following relationship from a Student to a Teacher. The follow target
/// is addressed by a Teacher Platform publicId (the public identity used to browse the
/// platform) and resolves to that platform's owner Teacher. Following is central and
/// independent of any tenant; a duplicate follow is rejected and the database unique
/// constraint guarantees the (Student, Teacher) pair cannot repeat.
/// </summary>
public sealed class FollowTeacherService
{
    private readonly IPlatformRepository _platforms;
    private readonly IPlatformMembershipRepository _memberships;
    private readonly IStudentRepository _students;
    private readonly IStudentFollowRepository _follows;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public FollowTeacherService(
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

    public async Task FollowAsync(
        Guid studentId,
        string? teacherPublicId,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);
        var teacherId = await ResolveOwnerTeacherIdAsync(platform.Id, cancellationToken);

        if (await _follows.GetAsync(studentId, teacherId, cancellationToken) is not null)
        {
            throw new BusinessRuleViolationException("The student already follows this teacher.");
        }

        var follow = new StudentFollow(studentId, teacherId);
        try
        {
            student.AddFollow(follow);
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        _follows.Add(follow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Resolves the platform's owner teacher Id. The membership data is tenant-scoped, so the
    /// tenant context is scoped to the platform for the read and restored afterwards (mirrors
    /// <see cref="GetTeacherPlatformAccessService"/>).
    /// </summary>
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