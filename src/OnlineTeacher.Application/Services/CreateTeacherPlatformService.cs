using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.Permissions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Creates a Teacher Platform tenant with its Owner role, owner permissions, and the
/// owning teacher's membership. The whole graph is committed atomically in one SaveChanges.
/// The slug is normalized deterministically from the platform name; duplicate slugs are allowed.
/// </summary>
public sealed class CreateTeacherPlatformService
{
    private readonly ITeacherRepository _teachers;
    private readonly IPlatformRepository _platforms;
    private readonly IRoleRepository _roles;
    private readonly IPermissionRepository _permissions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CreateTeacherPlatformService(
        ITeacherRepository teachers,
        IPlatformRepository platforms,
        IRoleRepository roles,
        IPermissionRepository permissions,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _teachers = teachers;
        _platforms = platforms;
        _roles = roles;
        _permissions = permissions;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<CreateTeacherPlatformResult> CreateAsync(
        Guid teacherId,
        string? name,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var teacher = await _teachers.GetByIdAsync(teacherId, cancellationToken)
            ?? throw new NotFoundException("Teacher does not exist.");

        var platform = CreatePlatform(name);
        var role = new Role(platform.Id, PlatformRoles.Owner);

        foreach (var code in PlatformPermissions.All)
        {
            var permission = await _permissions.GetByCodeAsync(code, cancellationToken)
                ?? throw new BusinessRuleViolationException($"Permission '{code}' is not available.");
            role.AddPermission(permission);
        }

        var membership = new TeacherPlatformMembership(teacher.Id, platform.Id, role.Id, isOwner: true);
        AddMembership(teacher, membership);

        if (!_tenantContext.TrySetTenant(platform.Id))
        {
            throw new TenantMismatchException("A teacher tenant scope is already active.");
        }

        try
        {
            _platforms.Add(platform);
            _roles.Add(role);
            _teachers.AddMembership(membership);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            _tenantContext.Clear();
        }

        return new CreateTeacherPlatformResult(platform.Id, platform.PublicId.Value, platform.Slug.Value, platform.Status);
    }

    private static TeacherPlatform CreatePlatform(string? name)
    {
        try
        {
            return new TeacherPlatform(name ?? string.Empty, PublicId.Generate(), Slug.CreateFromName(name));
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }

    private static void AddMembership(Teacher teacher, TeacherPlatformMembership membership)
    {
        try
        {
            teacher.AddMembership(membership);
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }
    }
}