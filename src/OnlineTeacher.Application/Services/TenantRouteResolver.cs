using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Resolves a public teacher platform route as PublicId + Slug.
/// Slug is never an identity and is never queried alone; resolution is keyed only by PublicId.
/// </summary>
public sealed class TenantRouteResolver
{
    private readonly IPlatformRepository _platforms;

    public TenantRouteResolver(IPlatformRepository platforms)
    {
        _platforms = platforms;
    }

    public async Task<TenantRouteResolution> ResolveAsync(
        string? publicId,
        string? slug,
        CancellationToken cancellationToken = default)
    {
        var platformPublicId = TryParsePublicId(publicId);
        if (platformPublicId is null)
        {
            return TenantRouteResolution.NotFound;
        }

        var platform = await _platforms.GetByPublicIdAsync(platformPublicId, cancellationToken);
        if (platform is null)
        {
            return TenantRouteResolution.NotFound;
        }

        var canonicalSlug = platform.Slug.Value;
        if (string.Equals(slug?.Trim(), canonicalSlug, StringComparison.Ordinal))
        {
            return new TenantRouteResolution(
                TenantRouteStatus.Matched,
                platform.Id,
                platform.PublicId.Value,
                canonicalSlug);
        }

        return new TenantRouteResolution(
            TenantRouteStatus.Redirect,
            platform.Id,
            platform.PublicId.Value,
            canonicalSlug);
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