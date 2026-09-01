namespace OnlineTeacher.Application.Dtos;

public enum TenantRouteStatus
{
    NotFound,
    Matched,
    Redirect
}

/// <summary>
/// Outcome of teacher platform route resolution.
/// Invalid PublicIds and missing platforms resolve to <see cref="TenantRouteStatus.NotFound"/>;
/// a valid PublicId with a differing slug resolves to <see cref="TenantRouteStatus.Redirect"/>
/// carrying the canonical platform information required for a 301 response.
/// </summary>
public sealed record TenantRouteResolution(
    TenantRouteStatus Status,
    Guid? PlatformId,
    string? PublicId,
    string? CanonicalSlug)
{
    public static TenantRouteResolution NotFound { get; } = new(TenantRouteStatus.NotFound, null, null, null);
}