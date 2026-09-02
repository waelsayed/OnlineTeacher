using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Api.Authorization;

namespace OnlineTeacher.UnitTests.Api;

/// <summary>
/// Verifies the dynamic permission authorization model: the policy provider maps a
/// permission policy to a requirement, the handler grants only when the server-issued
/// permission claim is present, and the attribute builds the expected dynamic policy name.
/// </summary>
public sealed class PermissionAuthorizationTests
{
    private static PermissionHandler NewHandler() => new();

    private static AuthorizationHandlerContext NewContext(
        ClaimsPrincipal user,
        IAuthorizationRequirement requirement) =>
        new(new List<IAuthorizationRequirement> { requirement }, user, resource: null);

    private static ClaimsPrincipal UserWithPermission(string permission) =>
        new(new ClaimsIdentity(
            new[] { new Claim(JwtClaims.Permission, permission) },
            authenticationType: "Bearer"));

    private static ClaimsPrincipal UserWithoutPermission() =>
        new(new ClaimsIdentity(authenticationType: "Bearer"));

    [Fact]
    public async Task Handle_PermissionPresent_SucceedsRequirement()
    {
        var requirement = new PermissionRequirement("Platform.Access");
        var context = NewContext(UserWithPermission("Platform.Access"), requirement);

        await NewHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_PermissionMissing_DoesNotSucceed()
    {
        var requirement = new PermissionRequirement("Platform.Access");
        var context = NewContext(UserWithoutPermission(), requirement);

        await NewHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DifferentPermissionCode_DoesNotSucceed()
    {
        var requirement = new PermissionRequirement("Exam.Create");
        var context = NewContext(UserWithPermission("Platform.Access"), requirement);

        await NewHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_AdditionalPermissionCodes_SucceedsWhenPresent()
    {
        var user = new ClaimsIdentity(new[]
        {
            new Claim(JwtClaims.Permission, "Platform.Access"),
            new Claim(JwtClaims.Permission, "Exam.Create")
        }, authenticationType: "Bearer");
        var requirement = new PermissionRequirement("Exam.Create");
        var context = NewContext(new ClaimsPrincipal(user), requirement);

        await NewHandler().HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task PolicyProvider_PermissionPolicy_ReturnsPermissionRequirement()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("Permission:Platform.Access");

        policy.Should().NotBeNull();
        policy!.Requirements.Should().ContainSingle()
            .Which.Should().BeOfType<PermissionRequirement>()
            .Which.Permission.Should().Be("Platform.Access");
    }

    [Fact]
    public async Task PolicyProvider_UnknownPolicy_ForwardsToDefaultProvider()
    {
        var provider = new PermissionPolicyProvider(Options.Create(new AuthorizationOptions()));

        var policy = await provider.GetPolicyAsync("SomeOtherPolicy");

        policy.Should().BeNull(); // default provider has no pre-registered policy by that name
    }

    [Fact]
    public void RequirePermission_BuildsDynamicPolicyName()
    {
        var attribute = new RequirePermissionAttribute("Exam.Create");

        attribute.Permission.Should().Be("Exam.Create");
        attribute.Policy.Should().Be("Permission:Exam.Create");
    }
}