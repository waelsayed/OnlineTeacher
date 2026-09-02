using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.UnitTests.Api;

/// <summary>
/// Verifies the server-side JWT construction for the platform-scoped access profile:
/// claims carry only identity/authorization data (sub, tenant, isOwner, roles, permissions)
/// and never any password/hash/sensitive material, matching the approved claim set.
/// </summary>
public sealed class JwtTokenFactoryTests
{
    private const string SigningKey = "unit-tests-only-signing-key-0123456789abcdef-0123456789abcdef";
    private const string Issuer = "OnlineTeacher.UnitTests";
    private const string Audience = "OnlineTeacher.UnitTests";

    private static JwtTokenFactory CreateFactory(int tokenLifetimeMinutes = 120) =>
        new(Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            TokenLifetimeMinutes = tokenLifetimeMinutes
        }));

    private static TeacherPlatformAccess Access(
        Guid? teacherId = null,
        string publicId = "AbCdEf123456",
        bool isOwner = true,
        IReadOnlyList<string>? roleNames = null,
        IReadOnlyList<string>? permissions = null) =>
        new(
            teacherId ?? Guid.NewGuid(),
            Guid.NewGuid(),
            publicId,
            "my-platform",
            PlatformStatus.Active,
            isOwner,
            roleNames ?? ["Owner"],
            permissions ?? ["Platform.Access", "Platform.Manage"]);

    private static JwtSecurityToken ReadToken(string raw)
    {
        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(raw);
        return jsonToken;
    }

    [Fact]
    public void Create_EmitsSubjectTeacherId()
    {
        var teacherId = Guid.NewGuid();
        var access = Access(teacherId);

        var raw = CreateFactory().Create(access);
        var token = ReadToken(raw);

        token.Subject.Should().Be(teacherId.ToString());
    }

    [Fact]
    public void Create_EmitsTenantPublicIdClaim()
    {
        var access = Access(publicId: "ZzYyXx987654");

        var raw = CreateFactory().Create(access);
        var token = ReadToken(raw);

        token.Claims.Should().Contain(c =>
            c.Type == JwtClaims.Tenant && c.Value == "ZzYyXx987654");
    }

    [Fact]
    public void Create_EmitsIsOwnerClaim()
    {
        var ownerAccess = Access(isOwner: true);
        var memberAccess = Access(isOwner: false);

        var ownerToken = ReadToken(CreateFactory().Create(ownerAccess));
        var memberToken = ReadToken(CreateFactory().Create(memberAccess));

        ownerToken.Claims.Should().Contain(c => c.Type == JwtClaims.IsOwner && c.Value == "true");
        memberToken.Claims.Should().Contain(c => c.Type == JwtClaims.IsOwner && c.Value == "false");
    }

    [Fact]
    public void Create_EmitsAllRoleClaims()
    {
        var access = Access(roleNames: ["Owner", "Assistant"]);

        var token = ReadToken(CreateFactory().Create(access));

        token.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value)
            .Should().Equal("Owner", "Assistant");
    }

    [Fact]
    public void Create_EmitsAllPermissionClaims()
    {
        var access = Access(permissions: ["Platform.Access", "Platform.Manage", "Exam.Create"]);

        var token = ReadToken(CreateFactory().Create(access));

        token.Claims.Where(c => c.Type == JwtClaims.Permission).Select(c => c.Value)
            .Should().Equal("Platform.Access", "Platform.Manage", "Exam.Create");
    }

    [Fact]
    public void Create_UsesConfiguredIssuerAndAudience()
    {
        var token = ReadToken(CreateFactory().Create(Access()));

        token.Issuer.Should().Be(Issuer);
        token.Audiences.Should().Contain(Audience);
    }

    [Fact]
    public void Create_SetsConfiguredTokenLifetime()
    {
        var token = ReadToken(CreateFactory(45).Create(Access()));

        token.ValidFrom.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(30));
        token.ValidTo.Should().BeCloseTo(DateTime.UtcNow.AddMinutes(45), TimeSpan.FromSeconds(30));
    }

    [Fact]
    public void Create_WritesConfiguredKeyIdHeader()
    {
        var handler = new JwtSecurityTokenHandler();

        var token = handler.ReadJwtToken(CreateFactory().Create(Access()));

        token.Header["kid"]?.ToString().Should().Be("OnlineTeacher.SigningKey");
    }

    [Fact]
    public void Create_NeverEmitsPasswordOrHash()
    {
        var raw = CreateFactory().Create(Access());

        raw.ToLowerInvariant().Should().NotContain("password");
        raw.ToLowerInvariant().Should().NotContain("hash");
        raw.Should().NotContain("PasswordHash");
    }
}