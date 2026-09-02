using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.Permissions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Adds a teacher as a member of the resolved Teacher Platform. The acting user must be the
/// platform's owner. The target teacher is looked up by their central email, joined with a
/// non-owner Assistant role (created on first use with only the Platform.Access permission),
/// and the new membership is committed atomically with the (possibly new) role.
/// </summary>
public sealed class AddPlatformMemberService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IPlatformMembershipRepository _memberships;
    private readonly ITeacherRepository _teachers;
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IUnitOfWork _unitOfWork;

    public AddPlatformMemberService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IPlatformMembershipRepository memberships,
        ITeacherRepository teachers,
        IRoleRepository roles,
        IPermissionRepository permissions,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _memberships = memberships;
        _teachers = teachers;
        _roles = roles;
        _permissions = permissions;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlatformMember> AddAsync(
        Guid actorTeacherId,
        string? publicId,
        string? teacherEmail,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireOwnerAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var teacher = await FindTeacherAsync(teacherEmail, cancellationToken);

        if (teacher.Id == actorTeacherId)
        {
            throw new BusinessRuleViolationException("The platform owner is already a member.");
        }

        if (await _memberships.GetForTeacherAsync(platform.Id, teacher.Id, cancellationToken) is not null)
        {
            throw new BusinessRuleViolationException("The teacher is already a member of this platform.");
        }

        var role = await GetOrCreateAssistantRoleAsync(platform.Id, cancellationToken);

        var membership = new TeacherPlatformMembership(teacher.Id, platform.Id, role.Id, isOwner: false);
        try
        {
            teacher.AddMembership(membership);
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        _teachers.AddMembership(membership);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlatformMember(teacher.Id, teacher.Name, role.Id, role.Name, IsOwner: false);
    }

    private async Task<Teacher> FindTeacherAsync(string? teacherEmail, CancellationToken cancellationToken)
    {
        try
        {
            var email = Email.Create(teacherEmail ?? string.Empty);
            return await _teachers.GetByEmailAsync(email, cancellationToken)
                ?? throw new ValidationException("The specified teacher does not exist.");
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }

    private async Task<Role> GetOrCreateAssistantRoleAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var existing = await _roles.GetByNameAsync(tenantId, PlatformRoles.Assistant, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var role = new Role(tenantId, PlatformRoles.Assistant);
        var permission = await _permissions.GetByCodeAsync(PlatformPermissions.Access, cancellationToken)
            ?? throw new BusinessRuleViolationException($"Permission '{PlatformPermissions.Access}' is not available.");
        role.AddPermission(permission);

        _roles.Add(role);
        return role;
    }
}