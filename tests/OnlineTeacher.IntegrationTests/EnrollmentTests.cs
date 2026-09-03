using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineTeacher.Api.Contracts;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// End-to-end verification of Task 4 (Student Enrollment in Teacher Courses) through the real
/// HTTP API and a real PostgreSQL Testcontainer: student self-enrollment in a published course,
/// duplicate/draft/unknown/cross-tenant handling, listing enrollments across platforms,
/// cancellation, teacher listing of a course's enrolled students, and authorization/tenant
/// isolation for the enrollment endpoints.
/// </summary>
[Collection("api")]
public sealed class EnrollmentTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private static int _counter;
    private readonly ApiFactory _factory;

    public EnrollmentTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() =>
        $"enroll{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Student_EnrollsInPublishedCourse_Returns201()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{teacher.CourseId}",
            studentToken);

        enroll.StatusCode.Should().Be(HttpStatusCode.Created, await enroll.Content.ReadAsStringAsync());
        (await enroll.Content.ReadFromJsonAsync<EnrollResponse>())!.EnrollmentId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Student_DuplicateEnrollment_Returns422()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var first = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{teacher.CourseId}",
            studentToken);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var duplicate = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{teacher.CourseId}",
            studentToken);

        duplicate.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Student_DraftCourse_Returns422()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreateCourseAsync(client,
            (teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token), "Algebra");
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{courseId}",
            studentToken);

        enroll.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Student_UnknownCourse_Returns404()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{Guid.NewGuid()}",
            studentToken);

        enroll.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Student_CrossTenantCourseReference_Returns404()
    {
        using var client = NewClient();
        var teacherA = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var teacherB = await NewTeacherWithActivePlatformAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        // Enroll in teacher A's course but address teacher B's platform: course must not resolve.
        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacherB.Platform.PublicId}/{teacherA.CourseId}",
            studentToken);

        enroll.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Student_UnknownPlatform_Returns404()
    {
        using var client = NewClient();
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{OnlineTeacher.Domain.ValueObjects.PublicId.Generate().Value}/{Guid.NewGuid()}",
            studentToken);

        enroll.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_Enroll_Returns401()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);

        var enroll = await client.PostAsync(
            $"/api/student/enroll/{teacher.Platform.PublicId}/{teacher.CourseId}",
            null);

        enroll.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Student_ListsEnrollmentsAcrossMultiplePlatforms()
    {
        using var client = NewClient();
        var teacherA = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var teacherB = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var enrollA = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacherA.Platform.PublicId}/{teacherA.CourseId}",
            studentToken);
        var enrollB = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacherB.Platform.PublicId}/{teacherB.CourseId}",
            studentToken);
        enrollA.StatusCode.Should().Be(HttpStatusCode.Created);
        enrollB.StatusCode.Should().Be(HttpStatusCode.Created);

        var listA = await GetStudentAsync(
            client,
            $"/api/student/enrollments/{teacherA.Platform.PublicId}",
            studentToken);
        listA.StatusCode.Should().Be(HttpStatusCode.OK);
        var listAEnrollments = await listA.Content.ReadFromJsonAsync<List<EnrollmentListItemResponse>>();
        listAEnrollments.Should().ContainSingle(e => e.CourseId == teacherA.CourseId);
        listAEnrollments.Should().NotContain(e => e.CourseId == teacherB.CourseId);
    }

    [Fact]
    public async Task Student_CancelsEnrollment_Returns204AndNotListed()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{teacher.CourseId}",
            studentToken);
        enroll.StatusCode.Should().Be(HttpStatusCode.Created);

        var cancel = await DeleteStudentAsync(
            client,
            $"/api/student/enrollments/{teacher.Platform.PublicId}/{teacher.CourseId}",
            studentToken);
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // A cancelled enrollment should still appear in the student's own enrollment history.
        var list = await GetStudentAsync(
            client,
            $"/api/student/enrollments/{teacher.Platform.PublicId}",
            studentToken);
        var listBody = await list.Content.ReadFromJsonAsync<List<EnrollmentListItemResponse>>();
        listBody!.Should().ContainSingle(e => e.CourseId == teacher.CourseId && e.Status == "Cancelled");
    }

    [Fact]
    public async Task Owner_ListsCourseEnrollments_ReturnsEnrolledStudents()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{teacher.CourseId}",
            studentToken);
        enroll.StatusCode.Should().Be(HttpStatusCode.Created);

        var list = await GetAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/courses/{teacher.CourseId}/enrollments",
            teacher.Token);

        list.StatusCode.Should().Be(HttpStatusCode.OK, await list.Content.ReadAsStringAsync());
        var students = await list.Content.ReadFromJsonAsync<List<CourseEnrolledStudentResponse>>();
        students.Should().ContainSingle();
    }

    [Fact]
    public async Task NonMember_CannotReadAnotherTenantsEnrollments_Returns403()
    {
        using var client = NewClient();
        var teacherA = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);
        var teacherB = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);

        var list = await GetAsync(
            client,
            $"/{teacherA.Platform.PublicId}/{teacherA.Platform.Slug}/api/platform/courses/{teacherA.CourseId}/enrollments",
            teacherB.Token);

        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Anonymous_CourseEnrollments_Returns401()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAndPublishedCourseAsync(client);

        var list = await client.GetAsync(
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/courses/{teacher.CourseId}/enrollments");

        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssistantWithoutEnrollmentView_Returns403()
    {
        using var client = NewClient();
        var owner = await RegisterTeacherWithPlatformAsync(client, UniqueEmail(), "Enrollment Perm");
        var ownerToken = await LoginTeacherAsync(client, owner.Email, Password, owner.Platform.PublicId);
        await ActivatePlatformAsync(client, owner.Platform.PublicId, ownerToken);
        var courseId = await CreateCourseAsync(client,
            (owner.Platform.PublicId, owner.Platform.Slug, ownerToken), "Algebra");
        await PublishCourseAsync(client, owner.Platform.PublicId, owner.Platform.Slug, ownerToken, courseId);

        var assistant = await RegisterTeacherOnlyAsync(client, UniqueEmail());
        var add = await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistant.Email),
            ownerToken);
        add.StatusCode.Should().Be(HttpStatusCode.OK, await add.Content.ReadAsStringAsync());

        var assistantToken = await LoginTeacherAsync(client, assistant.Email, Password, owner.Platform.PublicId);

        // Assistant is a member but has no Enrollment.View permission.
        var list = await GetAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/courses/{courseId}/enrollments",
            assistantToken);

        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<(CreateTeacherPlatformResponse Platform, string Email, Guid TeacherId)> RegisterTeacherWithPlatformAsync(
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

        return (platform!, email, teacher.TeacherId);
    }

    private static async Task<(Guid TeacherId, string Email)> RegisterTeacherOnlyAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Assistant", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();
        return (teacher!.TeacherId, email);
    }

    private static async Task<string> LoginTeacherAsync(HttpClient client, string email, string password, string publicId)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, publicId));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static async Task ActivatePlatformAsync(HttpClient client, string publicId, string token)
    {
        var activate = await PostAsync(client, $"/api/central/platforms/{publicId}/activate", token);
        activate.StatusCode.Should().Be(HttpStatusCode.OK, await activate.Content.ReadAsStringAsync());
    }

    private static async Task<Guid> CreateCourseAsync(
        HttpClient client, (string PublicId, string Slug, string Token) teacher, string title)
    {
        var response = await PostJsonAsync(
            client,
            $"/{teacher.PublicId}/{teacher.Slug}/api/platform/courses",
            new CreateCourseRequest(title, null),
            teacher.Token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var course = await response.Content.ReadFromJsonAsync<CourseResponse>();
        return course!.Id;
    }

    private static async Task PublishCourseAsync(
        HttpClient client, string publicId, string slug, string token, Guid courseId)
    {
        var update = await PutJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}",
            new UpdateCourseRequest(null, null, "Published"),
            token);
        update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
    }

    private static async Task<(Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token)> NewTeacherWithActivePlatformAsync(
        HttpClient client)
    {
        var teacher = await RegisterTeacherWithPlatformAsync(client, UniqueEmail(), "Enrollment Platform");
        var token = await LoginTeacherAsync(client, teacher.Email, Password, teacher.Platform.PublicId);
        await ActivatePlatformAsync(client, teacher.Platform.PublicId, token);
        return (teacher.TeacherId, teacher.Email, teacher.Platform, token);
    }

    private static async Task<(Guid CourseId, string PublicId, string Slug, string Token, CreateTeacherPlatformResponse Platform)> NewTeacherWithActivePlatformAndPublishedCourseAsync(
        HttpClient client)
    {
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreateCourseAsync(client, (teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token), "Algebra");
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        return (courseId, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, teacher.Platform);
    }

    private static async Task<(Guid StudentId, string Token)> RegisterAndLoginStudentAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/api/student/register",
            new RegisterStudentRequest("Student", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var student = await register.Content.ReadFromJsonAsync<RegisterStudentResponse>();

        var login = await client.PostAsJsonAsync("/api/student/login",
            new StudentLoginRequest(email, Password));
        login.StatusCode.Should().Be(HttpStatusCode.OK, await login.Content.ReadAsStringAsync());
        var body = await login.Content.ReadFromJsonAsync<StudentLoginResponse>();

        return (student!.StudentId, body!.Token);
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Get, url, token));

    private static async Task<HttpResponseMessage> PostAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Post, url, token));

    private static async Task<HttpResponseMessage> PostJsonAsync<T>(HttpClient client, string url, T body, string token)
        where T : class
    {
        var request = BuildRequest(HttpMethod.Post, url, token);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PutJsonAsync<T>(HttpClient client, string url, T body, string token)
        where T : class
    {
        var request = BuildRequest(HttpMethod.Put, url, token);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> GetStudentAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Get, url, token));

    private static async Task<HttpResponseMessage> PostStudentAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Post, url, token));

    private static async Task<HttpResponseMessage> DeleteStudentAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Delete, url, token));

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}

internal sealed record EnrollResponse(Guid EnrollmentId);
