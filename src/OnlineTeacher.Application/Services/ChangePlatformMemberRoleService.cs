using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.Permissions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Changes a member's role in the resolved Teacher Platform. The acting user must be the
/// platform's owner. Ownership is never removed from the last remaining owner, so the
/// platform always keeps a valid owner.
/// </summary>
public sealed class ChangePlatformMemberRoleService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IPlatformMembershipRepository _memberships;
    private readonly IRoleRepository _roles;
    private readonly ITeacherRepository _teachers;
    private readonly IUnitOfWork _unitOfWork;

    public ChangePlatformMemberRoleService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IPlatformMembershipRepository memberships,
        IRoleRepository roles,
        ITeacherRepository teachers,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _memberships = memberships;
        _roles = roles;
        _teachers = teachers;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlatformMember> ChangeAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid memberTeacherId,
        string? roleName,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireOwnerAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var membership = await _memberships.GetForTeacherAsync(platform.Id, memberTeacherId, cancellationToken)
            ?? throw new NotFoundException("The member is not part of this platform.");

        var role = await FindRoleAsync(platform.Id, roleName, cancellationToken);
        var becomesOwner = string.Equals(role.Name, PlatformRoles.Owner, StringComparison.Ordinal);

        if (membership.IsOwner && !becomesOwner)
        {
            await EnsureAnotherOwnerExistsAsync(platform.Id, memberTeacherId, cancellationToken);
        }

        try
        {
            membership.ChangeRole(role.Id, becomesOwner);
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var teacherName = (await _teachers.GetByIdAsync(memberTeacherId, cancellationToken))?.Name ?? memberTeacherId.ToString();

        return new PlatformMember(memberTeacherId, teacherName, role.Id, role.Name, becomesOwner);
    }

    private async Task<Role> FindRoleAsync(Guid tenantId, string? roleName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ValidationException("A role name is required.");
        }

        var role = await _roles.GetByNameAsync(tenantId, roleName.Trim(), cancellationToken);
        return role ?? throw new ValidationException("The specified role does not exist in this platform.");
    }

    private async Task EnsureAnotherOwnerExistsAsync(Guid tenantId, Guid ownerTeacherId, CancellationToken cancellationToken)
    {
        var members = await _memberships.GetMembersAsync(tenantId, cancellationToken);
        var hasAnotherOwner = members.Any(m => m.IsOwner && m.TeacherId != ownerTeacherId);

        if (!hasAnotherOwner)
        {
            throw new BusinessRuleViolationException("The platform must retain at least one owner.");
        }
    }
}