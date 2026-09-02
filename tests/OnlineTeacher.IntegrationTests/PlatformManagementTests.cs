using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineTeacher.Api.Contracts;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// End-to-end verification of Task 1 (Teacher Platform Management) through the real HTTP API
/// and a real PostgreSQL Testcontainer: owner profile get/update, membership list/add/change
/// role/remove, owner protection, and tenant isolation for management endpoints. Also re-asserts
/// that anonymous requests, invalid PublicId, and wrong-slug routing still behave as before.
/// </summary>
[Collection("api")]
public sealed class PlatformManagementTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private static int _counter;
    private readonly ApiFactory _factory;

    public PlatformManagementTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() =>
        $"mgr{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Owner_GetProfile_ReturnsManagedPlatformData()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail(), "Manage Profile");
        var token = await LoginAsync(client, teacher.Email, Password, teacher.Platform.PublicId);

        var response = await GetAsync(client, $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/profile", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await response.Content.ReadFromJsonAsync<PlatformProfileResponse>();
        profile!.PublicId.Should().Be(teacher.Platform.PublicId);
        profile.Name.Should().Be("Manage Profile");
        profile.Slug.Should().Be(teacher.Platform.Slug);
    }

    [Fact]
    public async Task Owner_UpdateProfile_UpdatesNameAndSlug()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail(), "Original Name");
        var token = await LoginAsync(client, teacher.Email, Password, teacher.Platform.PublicId);
        var url = $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/profile";

        var update = await PutJsonAsync(client, url, new UpdatePlatformProfileRequest("Updated Name", "updated-slug"), token);

        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<PlatformProfileResponse>();
        updated!.Name.Should().Be("Updated Name");
        updated.Slug.Should().Be("updated-slug");

        var fetch = await GetAsync(client, $"/{teacher.Platform.PublicId}/updated-slug/api/platform/profile", token);
        fetch.StatusCode.Should().Be(HttpStatusCode.OK);
        var reRead = await fetch.Content.ReadFromJsonAsync<PlatformProfileResponse>();
        reRead!.Name.Should().Be("Updated Name");
    }

    [Fact]
    public async Task Owner_UpdateProfile_EmptyBody_Returns400()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail(), "No Changes");
        var token = await LoginAsync(client, teacher.Email, Password, teacher.Platform.PublicId);

        var response = await PutJsonAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/profile",
            new UpdatePlatformProfileRequest(null, null),
            token);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AssistantMember_OwnerOnlyOperation_Returns403()
    {
        using var client = NewClient();
        var owner = await RegisterTeacherAsync(client, UniqueEmail(), "Assistant Owner");
        var assistant = await RegisterTeacherOnlyAsync(client, UniqueEmail());
        var ownerToken = await LoginAsync(client, owner.Email, Password, owner.Platform.PublicId);

        var add = await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistant.Email),
            ownerToken);
        add.StatusCode.Should().Be(HttpStatusCode.OK);

        var assistantToken = await LoginAsync(client, assistant.Email, Password, owner.Platform.PublicId);

        var response = await GetAsync(client, $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/profile", assistantToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonMember_ManageAnotherTenant_Returns403()
    {
        using var client = NewClient();
        var teacherA = await RegisterTeacherAsync(client, UniqueEmail(), "Tenant A");
        var teacherB = await RegisterTeacherAsync(client, UniqueEmail(), "Tenant B");
        var tokenA = await LoginAsync(client, teacherA.Email, Password, teacherA.Platform.PublicId);

        var response = await GetAsync(client, $"/{teacherB.Platform.PublicId}/{teacherB.Platform.Slug}/api/platform/profile", tokenA);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_ManagementRequest_Returns401()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail(), "Anonymous Manage");

        var response = await client.GetAsync($"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task InvalidPublicId_ManagementRequest_Returns404()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail(), "Invalid Manage");
        var token = await LoginAsync(client, teacher.Email, Password, teacher.Platform.PublicId);

        var response = await GetAsync(client, $"/InvalidPublicId00/api/platform/profile", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WrongSlug_ManagementRequest_Returns301CanonicalRedirect()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail(), "Wrong Slug Manage");
        var token = await LoginAsync(client, teacher.Email, Password, teacher.Platform.PublicId);

        var response = await GetAsync(client, $"/{teacher.Platform.PublicId}/old-wrong-slug/api/platform/profile", token);

        response.StatusCode.Should().Be(HttpStatusCode.MovedPermanently);
        response.Headers.Location!.ToString().Should().Be($"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/profile");
    }

    [Fact]
    public async Task Owner_AddAssistantThenListMembers_Succeeds()
    {
        using var client = NewClient();
        var owner = await RegisterTeacherAsync(client, UniqueEmail(), "Members List");
        var assistant = await RegisterTeacherOnlyAsync(client, UniqueEmail());
        var ownerToken = await LoginAsync(client, owner.Email, Password, owner.Platform.PublicId);

        var add = await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistant.Email),
            ownerToken);
        add.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await GetAsync(client, $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members", ownerToken);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var members = await list.Content.ReadFromJsonAsync<PlatformMemberResponse[]>();
        members!.Should().HaveCount(2);
        members.Should().Contain(m => m.RoleName == "Owner");
        members.Should().Contain(m => m.RoleName == "Assistant");
    }

    [Fact]
    public async Task Owner_ChangeAssistantRoleToOwner_Succeeds()
    {
        using var client = NewClient();
        var owner = await RegisterTeacherAsync(client, UniqueEmail(), "Promote");
        var assistant = await RegisterTeacherOnlyAsync(client, UniqueEmail());
        var ownerToken = await LoginAsync(client, owner.Email, Password, owner.Platform.PublicId);

        await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistant.Email),
            ownerToken);

        var change = await PutJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members/{assistant.TeacherId}",
            new ChangePlatformMemberRoleRequest("Owner"),
            ownerToken);
        change.StatusCode.Should().Be(HttpStatusCode.OK);

        var promoted = await change.Content.ReadFromJsonAsync<PlatformMemberResponse>();
        promoted!.IsOwner.Should().BeTrue();
        promoted.RoleName.Should().Be("Owner");
    }

    [Fact]
    public async Task Owner_CannotRemoveLastOwner_Returns422()
    {
        using var client = NewClient();
        var owner = await RegisterTeacherAsync(client, UniqueEmail(), "Last Owner");
        var ownerToken = await LoginAsync(client, owner.Email, Password, owner.Platform.PublicId);

        var remove = await client.SendAsync(BuildRequest(
            HttpMethod.Delete,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members/{owner.TeacherId}",
            ownerToken));

        remove.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Owner_RemovesAssistantMember_Returns204()
    {
        using var client = NewClient();
        var owner = await RegisterTeacherAsync(client, UniqueEmail(), "Remove Member");
        var assistant = await RegisterTeacherOnlyAsync(client, UniqueEmail());
        var ownerToken = await LoginAsync(client, owner.Email, Password, owner.Platform.PublicId);

        await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistant.Email),
            ownerToken);

        var remove = await client.SendAsync(BuildRequest(
            HttpMethod.Delete,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members/{assistant.TeacherId}",
            ownerToken));

        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var list = await GetAsync(client, $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members", ownerToken);
        var members = await list.Content.ReadFromJsonAsync<PlatformMemberResponse[]>();
        members!.Should().ContainSingle(m => m.RoleName == "Owner");
    }

    private static async Task<(Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform)> RegisterTeacherAsync(
        HttpClient client, string email, string platformName)
    {
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Teacher", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();

        var create = await client.PostAsJsonAsync("/api/central/platforms",
            new CreateTeacherPlatformRequest(teacher!.TeacherId, platformName));
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var platform = await create.Content.ReadFromJsonAsync<CreateTeacherPlatformResponse>();

        return (teacher.TeacherId, email, platform!);
    }

    private static async Task<(Guid TeacherId, string Email)> RegisterTeacherOnlyAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Assistant", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();
        return (teacher!.TeacherId, email);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password, string publicId)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, publicId));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string token)
    {
        var request = BuildRequest(HttpMethod.Get, url, token);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PutJsonAsync<T>(HttpClient client, string url, T body, string token)
        where T : class
    {
        var request = BuildRequest(HttpMethod.Put, url, token);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostJsonAsync<T>(HttpClient client, string url, T body, string token)
        where T : class
    {
        var request = BuildRequest(HttpMethod.Post, url, token);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}