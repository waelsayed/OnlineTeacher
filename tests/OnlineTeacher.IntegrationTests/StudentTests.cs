using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using OnlineTeacher.Api.Contracts;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// End-to-end verification of the central Student identity and following through the real HTTP
/// API and PostgreSQL Testcontainer: central registration/login (no Platform PublicId), the
/// Student JWT principal distinction, follow/unfollow/list/is-following, cross-tenant public
/// access, and the enforcement that a Student never gains Teacher Platform management rights.
/// </summary>
[Collection("api")]
public sealed class StudentTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private const string StudentPassword = "student-password-1234";
    private static int _counter;
    private readonly ApiFactory _factory;

    public StudentTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail(string prefix) =>
        $"{prefix}{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient();

    [Fact]
    public async Task Register_Succeeds()
    {
        using var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/student/register",
            new RegisterStudentRequest("Mona", UniqueEmail("stud"), StudentPassword));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<RegisterStudentResponse>();
        body!.StudentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409()
    {
        using var client = NewClient();
        var email = UniqueEmail("dup");
        var request = new RegisterStudentRequest("Mona", email, StudentPassword);

        await client.PostAsJsonAsync("/api/student/register", request);
        var duplicate = await client.PostAsJsonAsync("/api/student/register", request);

        duplicate.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_InvalidData_Returns400()
    {
        using var client = NewClient();

        var response = await client.PostAsJsonAsync("/api/student/register",
            new RegisterStudentRequest("  ", UniqueEmail("bad"), StudentPassword));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_SucceedsWithoutPlatformPublicId()
    {
        using var client = NewClient();
        var email = UniqueEmail("login");
        await client.PostAsJsonAsync("/api/student/register",
            new RegisterStudentRequest("Mona", email, StudentPassword));

        var login = await client.PostAsJsonAsync("/api/student/login",
            new StudentLoginRequest(email, StudentPassword));

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<StudentLoginResponse>();
        body!.Token.Should().NotBeNullOrWhiteSpace();
        body.StudentId.Should().NotBeEmpty();
        body.Token.Should().NotContain(StudentPassword);
        body.Token.Should().NotContain("hash");
    }

    [Fact]
    public async Task Login_InvalidCredentials_Returns401()
    {
        using var client = NewClient();
        var email = UniqueEmail("badlogin");
        await client.PostAsJsonAsync("/api/student/register",
            new RegisterStudentRequest("Mona", email, StudentPassword));

        var wrongPassword = await client.PostAsJsonAsync("/api/student/login",
            new StudentLoginRequest(email, "wrong-password"));
        var unknownEmail = await client.PostAsJsonAsync("/api/student/login",
            new StudentLoginRequest($"nobody-{Guid.NewGuid():N}@example.com", Password));

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_ReturnsProfile()
    {
        using var client = NewClient();
        var (studentId, token) = await RegisterAndLoginAsync(client, "me");

        var me = await GetStudentAsync(client, "/api/student/me", token);

        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await me.Content.ReadFromJsonAsync<StudentProfileResponse>();
        body!.StudentId.Should().Be(studentId);
        body.Name.Should().NotBeNullOrWhiteSpace();
        body.Email.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ProtectedStudentEndpoints_Unauthenticated_Returns401()
    {
        using var client = NewClient();

        var me = await client.GetAsync("/api/student/me");
        var following = await client.GetAsync("/api/student/following");

        me.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        following.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TeacherToken_CannotAccessStudentEndpoints()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail("tg"), "Teacher Platform");
        var teacherToken = await LoginTeacherAsync(client, teacher.Email, teacher.Platform.PublicId);

        var me = await GetStudentAsync(client, "/api/student/me", teacherToken);

        me.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Follow_Then_List_And_Unfollow_FullFlow()
    {
        using var client = NewClient();
        var (_, studentToken) = await RegisterAndLoginAsync(client, "flow");
        var teacherA = await RegisterTeacherAsync(client, UniqueEmail("fa"), "Follow A");
        var teacherB = await RegisterTeacherAsync(client, UniqueEmail("fb"), "Follow B");

        var followA = await PostStudentAsync(client, $"/api/student/follow/{teacherA.Platform.PublicId}", studentToken);
        var followB = await PostStudentAsync(client, $"/api/student/follow/{teacherB.Platform.PublicId}", studentToken);
        followA.StatusCode.Should().Be(HttpStatusCode.OK);
        followB.StatusCode.Should().Be(HttpStatusCode.OK);

        var following = await GetStudentAsync(client, "/api/student/following", studentToken);
        following.StatusCode.Should().Be(HttpStatusCode.OK);
        var list = await following.Content.ReadFromJsonAsync<List<FollowedTeacherResponse>>();
        list.Should().Contain(f => f.PublicId == teacherA.Platform.PublicId);
        list.Should().Contain(f => f.PublicId == teacherB.Platform.PublicId);
        list.Should().OnlyHaveUniqueItems();
        list!.Select(f => f.Slug).Should().NotContain(string.Empty);

        var isFollowingA = await GetStudentAsync(client, $"/api/student/following/{teacherA.Platform.PublicId}", studentToken);
        var isBodyA = await isFollowingA.Content.ReadFromJsonAsync<IsFollowingResponse>();
        isBodyA!.Follows.Should().BeTrue();

        var unfollow = await DeleteStudentAsync(client, $"/api/student/follow/{teacherA.Platform.PublicId}", studentToken);
        unfollow.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var isFollowingAfter = await GetStudentAsync(client, $"/api/student/following/{teacherA.Platform.PublicId}", studentToken);
        var isBodyAfter = await isFollowingAfter.Content.ReadFromJsonAsync<IsFollowingResponse>();
        isBodyAfter!.Follows.Should().BeFalse();
    }

    [Fact]
    public async Task DuplicateFollow_Returns422()
    {
        using var client = NewClient();
        var (_, studentToken) = await RegisterAndLoginAsync(client, "dupfollow");
        var teacher = await RegisterTeacherAsync(client, UniqueEmail("df"), "Dup Follow Platform");

        var first = await PostStudentAsync(client, $"/api/student/follow/{teacher.Platform.PublicId}", studentToken);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var duplicate = await PostStudentAsync(client, $"/api/student/follow/{teacher.Platform.PublicId}", studentToken);

        duplicate.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Unfollow_WhenNotFollowing_IsSafeNoOp()
    {
        using var client = NewClient();
        var (_, studentToken) = await RegisterAndLoginAsync(client, "unf");
        var teacher = await RegisterTeacherAsync(client, UniqueEmail("uf"), "Unfollow Platform");

        var unfollow = await DeleteStudentAsync(client, $"/api/student/follow/{teacher.Platform.PublicId}", studentToken);

        unfollow.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Student_CannotManageEitherTeachersPlatform()
    {
        using var client = NewClient();
        var (_, studentToken) = await RegisterAndLoginAsync(client, "cross");
        var teacherA = await RegisterTeacherAsync(client, UniqueEmail("pa"), "Public A");
        var teacherB = await RegisterTeacherAsync(client, UniqueEmail("pb"), "Public B");

        var meA = await GetStudentAsync(client, $"/{teacherA.Platform.PublicId}/{teacherA.Platform.Slug}/api/platform/me", studentToken);
        var meB = await GetStudentAsync(client, $"/{teacherB.Platform.PublicId}/{teacherB.Platform.Slug}/api/platform/me", studentToken);

        meA.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        meB.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task TeacherAuthentication_StillWorksUnchanged()
    {
        using var client = NewClient();
        var teacher = await RegisterTeacherAsync(client, UniqueEmail("tl"), "Teacher Still Works");

        var token = await LoginTeacherAsync(client, teacher.Email, teacher.Platform.PublicId);

        var me = await GetStudentAsync(client, $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/me", token);
        me.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task<(Guid StudentId, string Token)> RegisterAndLoginAsync(HttpClient client, string prefix)
    {
        var email = UniqueEmail(prefix);
        var register = await client.PostAsJsonAsync("/api/student/register",
            new RegisterStudentRequest("Student User", email, StudentPassword));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var registered = await register.Content.ReadFromJsonAsync<RegisterStudentResponse>();

        var login = await client.PostAsJsonAsync("/api/student/login",
            new StudentLoginRequest(email, StudentPassword));
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<StudentLoginResponse>();

        return (registered!.StudentId, body!.Token);
    }

    private static async Task<(Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform)> RegisterTeacherAsync(
        HttpClient client, string email, string platformName)
    {
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Teacher", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();

        var response = await client.PostAsJsonAsync("/api/central/platforms",
            new CreateTeacherPlatformRequest(teacher!.TeacherId, platformName));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        var platform = await response.Content.ReadFromJsonAsync<CreateTeacherPlatformResponse>();

        return (teacher.TeacherId, email, platform!);
    }

    private static async Task<string> LoginTeacherAsync(HttpClient client, string email, string publicId)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password, publicId));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static async Task<HttpResponseMessage> GetStudentAsync(HttpClient client, string url, string token) =>
        await SendAsync(client, HttpMethod.Get, url, token);

    private static async Task<HttpResponseMessage> PostStudentAsync(HttpClient client, string url, string token) =>
        await SendAsync(client, HttpMethod.Post, url, token);

    private static async Task<HttpResponseMessage> DeleteStudentAsync(HttpClient client, string url, string token) =>
        await SendAsync(client, HttpMethod.Delete, url, token);

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpMethod method, string url, string token)
    {
        var message = new HttpRequestMessage(method, url);
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(message);
    }
}

internal sealed record IsFollowingResponse(bool Follows);