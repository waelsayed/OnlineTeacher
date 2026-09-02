using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Updates the editable profile of the resolved Teacher Platform (Name and Slug) for an
/// authorized owner. The internal Id and the stable PublicId are immutable and never
/// changeable here. The actor must be a member of the tenant; membership mutations and the
/// ability to reach this management path are enforced by ownership plus permission policy.
/// </summary>
public sealed class UpdatePlatformProfileService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePlatformProfileService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _unitOfWork = unitOfWork;
    }

    public async Task<PlatformProfileResult> UpdateAsync(
        Guid actorTeacherId,
        string? publicId,
        string? name,
        string? slug,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireOwnerAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        if (name is not null && string.IsNullOrWhiteSpace(name))
        {
            throw new ValidationException("Platform name is required.");
        }

        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(slug))
        {
            throw new ValidationException("Provide a platform name and/or slug to update.");
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                platform.Rename(name);
            }

            if (!string.IsNullOrWhiteSpace(slug))
            {
                platform.ChangeSlug(Slug.Create(slug));
            }
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PlatformProfileResult(
            platform.Id,
            platform.PublicId.Value,
            platform.Name,
            platform.Slug.Value,
            platform.Status);
    }
}