using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.UnitTests.Api;

/// <summary>
/// Verifies the teacher/student principal-type separation: the factory emits the correct
/// principal_type claim on both teacher and student tokens, and the policy provider maps a
/// PrincipalType policy to a requirement that only a matching principal can satisfy.
/// </summary>
public sealed class PrincipalTypeTests
{
    private const string SigningKey = "unit-tests-only-signing-key-0123456789abcdef-0123456789abcdef";
    private const string Issuer = "OnlineTeacher.UnitTests";
    private const string Audience = "OnlineTeacher.UnitTests";

    private static JwtTokenFactory CreateFactory() =>
        new(Options.Create(new JwtOptions
        {
            Issuer = Issuer,
            Audience = Audience,
            SigningKey = SigningKey,
            TokenLifetimeMinutes = 120
        }));

    private static JwtSecurityToken ReadToken(string raw) => new JwtSecurityTokenHandler().ReadJwtToken(raw);

    private static TeacherPlatformAccess TeacherAccess() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "AbCdEf123456",
            "my-platform",
            PlatformStatus.Active,
            IsOwner: true,
            ["Owner"],
            ["Platform.Access"]);

    [Fact]
    public void CreateTeacherToken_EmitsTeacherPrincipalType()
    {
        var token = ReadToken(CreateFactory().Create(TeacherAccess()));

        token.Claims.Should().Contain(c =>
            c.Type == JwtClaims.PrincipalType && c.Value == PrincipalTypes.Teacher);
    }

    [Fact]
    public void CreateTeacherToken_DoesNotEmitStudentPrincipalType()
    {
        var token = ReadToken(CreateFactory().Create(TeacherAccess()));

        token.Claims.Should().NotContain(c =>
            c.Type == JwtClaims.PrincipalType && c.Value == PrincipalTypes.Student);
    }

    [Fact]
    public void CreateStudentToken_EmitsSubjectStudentId()
    {
        var studentId = Guid.NewGuid();

        var token = ReadToken(CreateFactory().CreateStudent(studentId));

        token.Subject.Should().Be(studentId.ToString());
    }

    [Fact]
    public void CreateStudentToken_EmitsStudentPrincipalType()
    {
        var token = ReadToken(CreateFactory().CreateStudent(Guid.NewGuid()));

        token.Claims.Should().Contain(c =>
            c.Type == JwtClaims.PrincipalType && c.Value == PrincipalTypes.Student);
    }

    [Fact]
    public void CreateStudentToken_DoesNotEmitTeacherPrincipalTypeOrTenant()
    {
        var token = ReadToken(CreateFactory().CreateStudent(Guid.NewGuid()));

        token.Claims.Should().NotContain(c =>
            c.Type == JwtClaims.PrincipalType && c.Value == PrincipalTypes.Teacher);
        token.Claims.Should().NotContain(c => c.Type == JwtClaims.Tenant);
    }

    private static PrincipalTypeHandler NewHandler() => new();

    private static AuthorizationHandlerContext NewContext(
        ClaimsPrincipal user,
        IAuthorizationRequirement requirement) =>
        new(new List<IAuthorizationRequirement> { requirement }, user, resource: null);

    private static ClaimsPrincipal UserOfType(string principalType) =>
        new(new ClaimsIdentity(
            new[] { new Claim(JwtClaims.PrincipalType, principalType) },
            authenticationType: "Bearer"));

    [Fact]
    public async Task Handler_MatchingPrincipalType_SucceedsRequirement()
    {
        var requirement = new PrincipalTypeRequirement(PrincipalTypes.Student);
        var context = NewContext(UserOfType(PrincipalTypes.Student), requirement);

        await NewHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handler_DifferentPrincipalType_DoesNotSucceed()
    {
        var requirement = new PrincipalTypeRequirement(PrincipalTypes.Student);
        var context = NewContext(UserOfType(PrincipalTypes.Teacher), requirement);

        await NewHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handler_MissingPrincipalTypeClaim_DoesNotSucceed()
    {
        var requirement = new PrincipalTypeRequirement(PrincipalTypes.Student);
        var context = NewContext(new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Bearer")), requirement);

        await NewHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task PolicyProvider_PrincipalTypePolicy_ReturnsPrincipalTypeRequirement()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("PrincipalType:student");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<PrincipalTypeRequirement>()
            .Which.PrincipalType.Should().Be(PrincipalTypes.Student);
    }

    [Fact]
    public void RequirePrincipalType_BuildsDynamicPolicyName()
    {
        var attribute = new RequirePrincipalTypeAttribute(PrincipalTypes.Student);

        attribute.PrincipalType.Should().Be(PrincipalTypes.Student);
        attribute.Policy.Should().Be("PrincipalType:student");
    }
}