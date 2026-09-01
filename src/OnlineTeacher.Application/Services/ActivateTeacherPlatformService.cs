using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Activates a pending Teacher Platform, recording the activation timestamp.
/// The only permitted transition is PendingActivation to Active.
/// </summary>
public sealed class ActivateTeacherPlatformService
{
    private readonly IPlatformRepository _platforms;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public ActivateTeacherPlatformService(
        IPlatformRepository platforms,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<ActivateTeacherPlatformResult> ActivateAsync(
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var platform = await _platforms.GetByPublicIdAsync(ParsePublicId(publicId), cancellationToken)
            ?? throw new NotFoundException("Teacher platform does not exist.");

        try
        {
            platform.Activate();
        }
        catch (DomainException exception)
        {
            throw new BusinessRuleViolationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ActivateTeacherPlatformResult(
            platform.Id,
            platform.PublicId.Value,
            platform.ActivatedAtUtc!.Value);
    }

    private static PublicId ParsePublicId(string? publicId)
    {
        try
        {
            return PublicId.Create(publicId ?? string.Empty);
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }
}