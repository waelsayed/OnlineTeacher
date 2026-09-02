using System.Security.Authentication;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Application.Tenancy;

namespace OnlineTeacher.Api.Middleware;

/// <summary>
/// Resolves the {publicId}/{slug} tenant route for protected API requests.
///
/// Behavior (fixed):
/// 1. PublicId not found / invalid -> 404
/// 2. PublicId found + supplied slug == canonical -> establish tenant context and continue
/// 3. PublicId found + supplied slug != canonical -> 301 permanent redirect to the canonical URL
/// 4. Slug is never an identity and is never resolved alone.
///
/// If TenantRouteStatus.Matched, the middleware sets the scoped tenant context for the
/// remainder of the request so subsequent authorization/data access are tenant-aware.
/// A controller may access its [FromRoute] arguments normally; no body rewriting is used.
/// </summary>
public sealed class TenantRouteMiddleware : IMiddleware
{
    private readonly TenantRouteResolver _resolver;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<TenantRouteMiddleware> _logger;

    public TenantRouteMiddleware(
        TenantRouteResolver resolver,
        ITenantContext tenantContext,
        ILogger<TenantRouteMiddleware> logger)
    {
        _resolver = resolver;
        _tenantContext = tenantContext;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var publicId = GetRouteValue(context, "publicId");
        var slug = GetRouteValue(context, "slug");

        if (publicId is null || slug is null)
        {
            await next(context);
            return;
        }

        var resolution = await _resolver.ResolveAsync(publicId, slug, context.RequestAborted);

        switch (resolution.Status)
        {
            case TenantRouteStatus.NotFound:
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(new
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Not Found",
                    Detail = "The requested teacher platform does not exist."
                });
                return;

            case TenantRouteStatus.Redirect:
                var canonical = ResolveCanonicalPath(context, slug, resolution.CanonicalSlug!);
                _logger.LogInformation(
                    "Redirecting tenant route to canonical URL {CanonicalUrl} from {PublicId}/{SuppliedSlug}",
                    canonical, publicId, slug);
                context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
                context.Response.Headers.Location = canonical;
                return;

            case TenantRouteStatus.Matched:
                EnforceTenantBinding(context, publicId);

                if (!_tenantContext.TrySetTenant(resolution.PlatformId!.Value))
                {
                    throw new AuthenticationException("The request could not be associated with a single tenant scope.");
                }

                await next(context);
                return;

            default:
                throw new AuthenticationException("Tenant resolution returned an unexpected result.");
        }
    }

    /// <summary>
    /// Centralized tenant-isolation enforcement at the security boundary. For any
    /// authenticated request to a resolved tenant route, the JWT <c>tenant</c> claim MUST
    /// equal the route <c>publicId</c>. Tenant isolation must not depend on each service or
    /// controller remembering to perform this check. Anonymous requests are intentionally
    /// allowed to reach the resolved route so downstream authorization returns 401 and the
    /// tenant context is still established for tenant-aware data access.
    /// </summary>
    private static void EnforceTenantBinding(HttpContext context, string publicId)
    {
        var principal = context.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var tokenTenant = principal.FindFirst(JwtClaims.Tenant)?.Value;
        if (tokenTenant is null)
        {
            throw new TenantMismatchException("The token is not scoped to a teacher platform.");
        }

        if (!string.Equals(tokenTenant, publicId, StringComparison.Ordinal))
        {
            throw new TenantMismatchException("The token is not authorized for this teacher platform.");
        }
    }

    private static string? GetRouteValue(HttpContext context, string key)
    {
        var value = context.Request.RouteValues.TryGetValue(key, out var raw) ? raw?.ToString() : null;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    /// <summary>
    /// Builds the canonical redirect path by substituting the canonical slug for the supplied
    /// slug while preserving any path that follows the slug segment (e.g. "/api/platform/me"),
    /// so the redirect resolves to the same endpoint rather than to a non-existent route.
    /// </summary>
    private static string ResolveCanonicalPath(HttpContext context, string suppliedSlug, string canonicalSlug)
    {
        var path = context.Request.Path.Value ?? "/";
        var marker = "/" + suppliedSlug;
        var index = path.IndexOf(marker, StringComparison.Ordinal);
        var suffix = index >= 0
            ? path[(index + marker.Length)..]
            : string.Empty;

        var routePrefix = path.Split('/')[1];
        return $"/{routePrefix}/{canonicalSlug}{suffix}";
    }
}