using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Reads a teacher's access profile for a Teacher Platform.
///
/// This is a read use case required by the API composition layer to (1) build
/// platform-scoped JWT claims at login and (2) verify server-side that an authenticated
/// teacher/membership is allowed to access the resolved tenant. It switches the tenant
/// context for the tenant-scoped read only when none is active, mirroring the pattern
/// already used by <see cref="CreateTeacherPlatformService"/>, and restores the prior scope.
/// </summary>
public sealed class GetTeacherPlatformAccessService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ITenantContext _tenantContext;

    public GetTeacherPlatformAccessService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _access = access;
        _tenantContext = tenantContext;
    }

    public async Task<TeacherPlatformAccess> GetAsync(
        Guid teacherId,
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        var platformPublicId = TryParsePublicId(publicId);
        if (platformPublicId is null)
        {
            throw new ValidationException("Public id is invalid.");
        }

        var platform = await _platforms.GetByPublicIdAsync(platformPublicId, cancellationToken)
            ?? throw new NotFoundException("Teacher platform does not exist.");

        return await GetWithinTenantAsync(teacherId, platform, cancellationToken);
    }

    private async Task<TeacherPlatformAccess> GetWithinTenantAsync(
        Guid teacherId,
        TeacherPlatform platform,
        CancellationToken cancellationToken)
    {
        var currentTenant = _tenantContext.TenantId;

        if (currentTenant.HasValue && currentTenant != platform.Id)
        {
            throw new TenantMismatchException("The teacher is not a member of this platform.");
        }

        try
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.TrySetTenant(platform.Id);
            }

            var access = await _access.GetAsync(teacherId, platform.Id, cancellationToken);
            return access ?? throw new TenantMismatchException("The teacher is not a member of this platform.");
        }
        finally
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.Clear();
            }
        }
    }

    private static PublicId? TryParsePublicId(string? publicId)
    {
        try
        {
            return PublicId.Create(publicId ?? string.Empty);
        }
        catch (DomainException)
        {
            return null;
        }
    }
}