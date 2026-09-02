using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Api.Middleware;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Api;

/// <summary>
/// Proves the tenant middleware resolves and scopes the tenant but does NOT reject an
/// authenticated request solely because its JWT "tenant" claim differs from the route tenant.
/// Cross-tenant access is enforced by downstream authorization and application security, not
/// by the middleware, so public (AllowAnonymous) tenant browsing can work for authenticated users.
/// </summary>
public sealed class TenantRouteMiddlewareTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly ITenantContext _tenantContext = new StubTenantContext();

    [Fact]
    public async Task Invoke_AuthNCrossTenantClaim_DoesNotRejectAndSetsTenantContext()
    {
        var platform = SeedPlatform();
        var resolver = new TenantRouteResolver(_platforms);
        var middleware = new TenantRouteMiddleware(
            resolver,
            _tenantContext,
            NullLogger<TenantRouteMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.RouteValues = new RouteValueDictionary
        {
            ["publicId"] = platform.PublicId.Value,
            ["slug"] = platform.Slug.Value
        };
        context.User = BuildAuthenticatedUser("DifferentTenantPublicId");

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        _tenantContext.TenantId.Should().Be(platform.Id);
    }

    [Fact]
    public async Task Invoke_Anonymous_DoesNotRejectAndSetsTenantContext()
    {
        var platform = SeedPlatform();
        var resolver = new TenantRouteResolver(_platforms);
        var middleware = new TenantRouteMiddleware(
            resolver,
            _tenantContext,
            NullLogger<TenantRouteMiddleware>.Instance);

        var context = new DefaultHttpContext();
        context.Request.RouteValues = new RouteValueDictionary
        {
            ["publicId"] = platform.PublicId.Value,
            ["slug"] = platform.Slug.Value
        };

        var nextCalled = false;
        await middleware.InvokeAsync(context, _ =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        });

        nextCalled.Should().BeTrue();
        _tenantContext.TenantId.Should().Be(platform.Id);
    }

    private TeacherPlatform SeedPlatform()
    {
        var platform = new TeacherPlatform(
            "Tenant B",
            PublicId.Generate(),
            Slug.CreateFromName("Tenant B"));
        _platforms.Seed(platform);
        return platform;
    }

    private static ClaimsPrincipal BuildAuthenticatedUser(string tenant)
    {
        var identity = new ClaimsIdentity(
            authenticationType: "Bearer",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);
        identity.AddClaim(new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString()));
        identity.AddClaim(new Claim(JwtClaims.Tenant, tenant));
        return new ClaimsPrincipal(identity);
    }
}