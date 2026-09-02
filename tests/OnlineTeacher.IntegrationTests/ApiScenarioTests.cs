using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;
using OnlineTeacher.Api.Contracts;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// End-to-end verification of the first vertical slice through the real HTTP API and a
/// real PostgreSQL Testcontainer: registration, platform creation, activation, login,
/// tenant routing/redirect, authorization and tenant isolation.
/// </summary>
[Collection("api")]
public sealed class ApiScenarioTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private static int _counter;
    private readonly ApiFactory _factory;

    public ApiScenarioTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() =>
        $"user{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task FullVerticalSlice_Succeeds()
    {
        using var client = NewClient();

        var teacher = await RegisterTeacherAsync(client, UniqueEmail(), "My Platform");
        var platform = teacher.Platform;
        platform.Status.Should().Be("PendingActivation");

        var token = await LoginAsync(client, teacher.Email, Password, platform.PublicId);

        var activate = await PostAsync(client, $"/api/central/platforms/{platform.PublicId}/activate", token);
        activate.StatusCode.Should().Be(HttpStatusCode.OK);
        var activated = await activate.Content.ReadFromJsonAsync<ActivateTeacherPlatformResponse>();
        activated!.ActivatedAtUtc.Should().NotBe(default);

        var me = await GetAsync(client, $"/{platform.PublicId}/{platform.Slug}/api/platform/me", token);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var meBody = await me.Content.ReadFromJsonAsync<PlatformMeResponse>();
        meBody!.TenantPublicId.Should().Be(platform.PublicId);
        meBody.Status.Should().Be("Active");
        meBody.IsOwner.Should().BeTrue();
        meBody.RoleNames.Should().Contain("Owner");
        meBody.PermissionCodes.Should().Contain("Platform.Access");
    }

    [Fact]
    public async Task Registration_DuplicateEmail_Returns409()
    {
        using var client = NewClient();

        var email = UniqueEmail();
        var body = new RegisterTeacherRequest("Wael Sayed", email, Password);
        await client.PostAsJsonAsync("/api/central/teachers/register", body);
        var duplicate = await client.PostAsJsonAsync("/api/central/teachers/register", body);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Registration_InvalidInput_Returns400()
    {
        using var client = NewClient();

        var invalid = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("  ", UniqueEmail(), Password));

        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsGeneric401()
    {
        using var client = NewClient();
        var registeredEmail = UniqueEmail();
        await RegisterTeacherOnlyAsync(client, registeredEmail);

        const string anyPublicId = "AbCdEf123456";

        var wrongPassword = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(registeredEmail, "wrong-password", anyPublicId));
        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertGenericBody(wrongPassword);

        var unknownEmail = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest($"nobody-{Guid.NewGuid():N}@example.com", Password, anyPublicId));
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await AssertGenericBody(unknownEmail);
    }

    private static async Task AssertGenericBody(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        body.Should().NotContain(Password);
        body.Should().NotContain("hash");
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsJwtWithoutSensitiveData()
    {
        using var client = NewClient();
        var found = await RegisterTeacherAsync(client, UniqueEmail(), "Login Platform");

        var login = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest(found.Email, Password, found.Platform.PublicId));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.PlatformId.Should().Be(found.Platform.PublicId);

        body.Token.Should().NotContain(Password);
        body.Token.Should().NotContain("hash");
    }

    [Fact]
    public async Task Platform_DuplicateSlug_IsAllowed()
    {
        using var client = NewClient();
        var a = await RegisterTeacherAsync(client, UniqueEmail(), "Shared Name");
        var b = await RegisterTeacherAsync(client, UniqueEmail(), "Shared Name");

        a.Platform.Slug.Should().Be(b.Platform.Slug);
    }

    [Fact]
    public async Task Route_InvalidPublicId_Returns404()
    {
        using var client = NewClient();
        var found = await RegisterTeacherAsync(client, UniqueEmail(), "Route Platform");
        var token = await LoginAsync(client, found.Email, Password, found.Platform.PublicId);

        var response = await GetAsync(client, "/InvalidPublicId00/api/platform/me", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Route_ValidPublicId_WrongSlug_Returns301CanonicalRedirect()
    {
        using var client = NewClient();
        var found = await RegisterTeacherAsync(client, UniqueEmail(), "Slug Platform");
        var token = await LoginAsync(client, found.Email, Password, found.Platform.PublicId);

        var response = await GetAsync(client, $"/{found.Platform.PublicId}/old-wrong-slug/api/platform/me", token);

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be($"/{found.Platform.PublicId}/{found.Platform.Slug}/api/platform/me");
    }

    [Fact]
    public async Task Route_ValidPublicId_CanonicalSlug_Succeeds()
    {
        using var client = NewClient();
        var found = await RegisterTeacherAsync(client, UniqueEmail(), "Canonical Platform");
        var token = await LoginAsync(client, found.Email, Password, found.Platform.PublicId);

        var response = await GetAsync(client, $"/{found.Platform.PublicId}/{found.Platform.Slug}/api/platform/me", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Authorization_Unauthenticated_Returns401()
    {
        using var client = NewClient();
        var found = await RegisterTeacherAsync(client, UniqueEmail(), "Unauth Platform");

        var response = await client.GetAsync($"/{found.Platform.PublicId}/{found.Platform.Slug}/api/platform/me");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TenantIsolation_TeacherA_CannotAccessTenantB()
    {
        using var client = NewClient();
        var ownerA = await RegisterTeacherAsync(client, UniqueEmail(), "Tenant A");
        var ownerB = await RegisterTeacherAsync(client, UniqueEmail(), "Tenant B");
        var tokenA = await LoginAsync(client, ownerA.Email, Password, ownerA.Platform.PublicId);

        var own = await GetAsync(client, $"/{ownerA.Platform.PublicId}/{ownerA.Platform.Slug}/api/platform/me", tokenA);
        own.StatusCode.Should().Be(HttpStatusCode.OK);

        var crossTenant = await GetAsync(client, $"/{ownerB.Platform.PublicId}/{ownerB.Platform.Slug}/api/platform/me", tokenA);
        crossTenant.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TenantIsolation_TokenMissingTenantClaim_Returns403()
    {
        using var client = NewClient();
        var found = await RegisterTeacherAsync(client, UniqueEmail(), "No Tenant Claim");
        var tokenWithoutTenant = CreateTokenWithoutTenantClaim(found.TeacherId);

        var response = await GetAsync(client, $"/{found.Platform.PublicId}/{found.Platform.Slug}/api/platform/me", tokenWithoutTenant);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SlugAlone_NeverResolvesPlatform()
    {
        using var client = NewClient();
        var found = await RegisterTeacherAsync(client, UniqueEmail(), "Slug Only");
        var token = await LoginAsync(client, found.Email, Password, found.Platform.PublicId);

        var slugOnly = await GetAsync(client, $"/{found.Platform.Slug}/api/platform/me", token);

        slugOnly.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<(Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform)> RegisterTeacherAsync(
        HttpClient client, string email, string platformName)
    {
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Teacher", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();

        var platform = await CreatePlatformAsync(client, teacher!.TeacherId, platformName);
        return (teacher.TeacherId, email, platform);
    }

    private static async Task<Guid> RegisterTeacherOnlyAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Teacher", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();
        return teacher!.TeacherId;
    }

    private static async Task<CreateTeacherPlatformResponse> CreatePlatformAsync(HttpClient client, Guid teacherId, string name)
    {
        var response = await client.PostAsJsonAsync("/api/central/platforms",
            new CreateTeacherPlatformRequest(teacherId, name));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var created = await response.Content.ReadFromJsonAsync<CreateTeacherPlatformResponse>();
        created.Should().NotBeNull();
        return created!;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password, string publicId)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, publicId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string token)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(message);
    }

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, string token)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(message);
    }

    /// <summary>
    /// Crafts a structurally valid JWT (correct issuer/audience/signing key/lifetime) that is
    /// authenticated by the bearer middleware but carries NO <c>tenant</c> claim, to prove the
    /// middleware rejects an authenticated token that cannot be bound to the tenant route.
    /// </summary>
    private static string CreateTokenWithoutTenantClaim(Guid teacherId)
    {
        const string signingKey = "integration-tests-only-signing-key-0123456789abcdef-0123456789abcdef";

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
            {
                KeyId = "OnlineTeacher.SigningKey"
            },
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "OnlineTeacher.IntegrationTests",
            audience: "OnlineTeacher.IntegrationTests",
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, teacherId.ToString())
            },
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}