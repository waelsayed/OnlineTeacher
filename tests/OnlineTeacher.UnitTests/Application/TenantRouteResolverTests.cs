using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class TenantRouteResolverTests
{
    private readonly FakePlatformRepository _platforms = new();

    private TenantRouteResolver CreateResolver() => new(_platforms);

    private TeacherPlatform SeedPlatform(string name = "My Platform", string? slug = null)
    {
        var platform = new TeacherPlatform(
            name,
            PublicId.Generate(),
            slug is null ? Slug.CreateFromName(name) : Slug.Create(slug));
        _platforms.Seed(platform);
        return platform;
    }

    [Fact]
    public async Task Resolve_InvalidPublicId_ReturnsNotFound()
    {
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync("not-a-valid-public-id", "my-platform");

        result.Status.Should().Be(TenantRouteStatus.NotFound);
    }

    [Fact]
    public async Task Resolve_UnknownPublicId_ReturnsNotFound()
    {
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(PublicId.Generate().Value, "my-platform");

        result.Status.Should().Be(TenantRouteStatus.NotFound);
    }

    [Fact]
    public async Task Resolve_MatchingPublicIdAndSlug_ReturnsMatched()
    {
        var platform = SeedPlatform();
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(platform.PublicId.Value, "my-platform");

        result.Status.Should().Be(TenantRouteStatus.Matched);
        result.PlatformId.Should().Be(platform.Id);
        result.PublicId.Should().Be(platform.PublicId.Value);
        result.CanonicalSlug.Should().Be("my-platform");
    }

    [Fact]
    public async Task Resolve_MatchingSlugWithSurroundingWhitespace_ReturnsMatched()
    {
        var platform = SeedPlatform();
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(platform.PublicId.Value, "  my-platform  ");

        result.Status.Should().Be(TenantRouteStatus.Matched);
    }

    [Fact]
    public async Task Resolve_MatchingPublicIdWrongSlug_ReturnsRedirectToCanonical()
    {
        var platform = SeedPlatform();
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(platform.PublicId.Value, "old-slug");

        result.Status.Should().Be(TenantRouteStatus.Redirect);
        result.PlatformId.Should().Be(platform.Id);
        result.PublicId.Should().Be(platform.PublicId.Value);
        result.CanonicalSlug.Should().Be("my-platform");
    }

    [Fact]
    public async Task Resolve_WrongSlugCase_ReturnsRedirect()
    {
        var platform = SeedPlatform();
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(platform.PublicId.Value, "MY-PLATFORM");

        result.Status.Should().Be(TenantRouteStatus.Redirect);
        result.CanonicalSlug.Should().Be("my-platform");
    }

    [Fact]
    public async Task Resolve_DuplicateSlugsDoNotAffectResolution()
    {
        var first = SeedPlatform(name: "My Platform");
        var second = SeedPlatform(name: "My Platform");
        var resolver = CreateResolver();

        var firstResult = await resolver.ResolveAsync(first.PublicId.Value, "my-platform");
        var secondResult = await resolver.ResolveAsync(second.PublicId.Value, "my-platform");

        firstResult.PlatformId.Should().Be(first.Id);
        secondResult.PlatformId.Should().Be(second.Id);
    }

    [Fact]
    public async Task Resolve_PlatformWithSameSlugAsAnotherButWrongCanonical_RedirectsToOwnCanonical()
    {
        var first = SeedPlatform(name: "My Platform");
        _ = SeedPlatform(name: "My Platform");
        var canonicalSlug = Slug.CreateFromName("My Platform").Value;
        var resolver = CreateResolver();

        var result = await resolver.ResolveAsync(first.PublicId.Value, "totally-different");

        result.Status.Should().Be(TenantRouteStatus.Redirect);
        result.PlatformId.Should().Be(first.Id);
        result.CanonicalSlug.Should().Be(canonicalSlug);
    }
}