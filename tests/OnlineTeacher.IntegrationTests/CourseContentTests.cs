using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineTeacher.Api.Contracts;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// End-to-end verification of Task 3 (Teacher Platform Course Content) through the real HTTP API
/// and a real PostgreSQL Testcontainer: course create/list/get/update/publish/delete, unit and
/// lesson management, ordering, authorization (anonymous, student, non-member, assistant without
/// permission), and tenant isolation for course-content endpoints.
/// </summary>
[Collection("api")]
public sealed class CourseContentTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private static int _counter;
    private readonly ApiFactory _factory;

    public CourseContentTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() =>
        $"course{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Owner_CreatesCourse_ReturnsDraft()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());

        var response = await PostJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses",
            new CreateCourseRequest("Algebra", "Intro to algebra"),
            token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var course = await response.Content.ReadFromJsonAsync<CourseResponse>();
        course!.Title.Should().Be("Algebra");
        course.Summary.Should().Be("Intro to algebra");
        course.Status.Should().Be("Draft");
    }

    [Fact]
    public async Task Owner_AddsUnitsAndLessons_ReturnsNestedDetail()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());
        var courseId = await CreateCourseAsync(client, publicId, slug, token, "Biology");

        var unitResponse = await PostJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}/units",
            new AddUnitRequest("Unit One", null),
            token);
        unitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var unit = await unitResponse.Content.ReadFromJsonAsync<CourseUnitResponse>();
        unit!.Position.Should().Be(1);

        var lessonResponse = await PostJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}/units/{unit.Id}/lessons",
            new AddLessonRequest("Lesson One", null),
            token);
        lessonResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var lesson = await lessonResponse.Content.ReadFromJsonAsync<CourseLessonResponse>();
        lesson!.Position.Should().Be(1);

        var detail = await GetAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}",
            token);

        detail.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await detail.Content.ReadFromJsonAsync<CourseDetailResponse>();
        body!.Units.Should().ContainSingle();
        body.Units[0].Id.Should().Be(unit.Id);
        body.Units[0].Lessons.Should().ContainSingle(l => l.Id == lesson.Id);
    }

    [Fact]
    public async Task Owner_ListCourses_ReturnsAll()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());
        await CreateCourseAsync(client, publicId, slug, token, "Algebra");
        await CreateCourseAsync(client, publicId, slug, token, "Chemistry");

        var response = await GetAsync(client, $"/{publicId}/{slug}/api/platform/courses", token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var courses = await response.Content.ReadFromJsonAsync<CourseListItemResponse[]>();
        courses!.Select(c => c.Title).Should().Equal("Algebra", "Chemistry");
    }

    [Fact]
    public async Task Owner_PublishCourse_ThenReadsPublished()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());
        var courseId = await CreateCourseAsync(client, publicId, slug, token, "Physics");

        var update = await PutJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}",
            new UpdateCourseRequest(null, null, "Published"),
            token);
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await update.Content.ReadFromJsonAsync<CourseResponse>();
        updated!.Status.Should().Be("Published");

        var detail = await GetAsync(client, $"/{publicId}/{slug}/api/platform/courses/{courseId}", token);
        var body = await detail.Content.ReadFromJsonAsync<CourseDetailResponse>();
        body!.Status.Should().Be("Published");
    }

    [Fact]
    public async Task Owner_DeleteCourse_Returns204AndRemoves()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());
        var courseId = await CreateCourseAsync(client, publicId, slug, token, "History");

        var delete = await client.SendAsync(BuildRequest(
            HttpMethod.Delete,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}",
            token));
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetch = await GetAsync(client, $"/{publicId}/{slug}/api/platform/courses/{courseId}", token);
        fetch.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Owner_UnknownCourse_Returns404()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());

        var response = await GetAsync(client, $"/{publicId}/{slug}/api/platform/courses/{Guid.NewGuid()}", token);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_CourseRequest_Returns401()
    {
        using var client = NewClient();
        var (publicId, slug, _) = await NewOwnerClientAsync(client, UniqueEmail());

        var response = await client.GetAsync($"/{publicId}/{slug}/api/platform/courses");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task StudentToken_CourseMutation_Returns403()
    {
        using var client = NewClient();
        var (publicId, slug, _) = await NewOwnerClientAsync(client, UniqueEmail());
        var student = await RegisterStudentAsync(client, UniqueEmail());
        var studentToken = await LoginStudentAsync(client, student.Email, Password);

        var response = await PostJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses",
            new CreateCourseRequest("Algebra", null),
            studentToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AssistantWithoutCoursePermission_ReadsCourses_Returns403()
    {
        using var client = NewClient();
        var owner = await RegisterTeacherWithPlatformAsync(client, UniqueEmail(), "Perm Check");
        var assistant = await RegisterTeacherOnlyAsync(client, UniqueEmail());
        var ownerToken = await LoginAsync(client, owner.Email, Password, owner.Platform.PublicId);

        var add = await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistant.Email),
            ownerToken);
        add.StatusCode.Should().Be(HttpStatusCode.OK);

        var assistantToken = await LoginAsync(client, assistant.Email, Password, owner.Platform.PublicId);

        var response = await GetAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/courses",
            assistantToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task NonMember_ReadAnotherTenantCourses_Returns403()
    {
        using var client = NewClient();
        var teacherA = await RegisterTeacherWithPlatformAsync(client, UniqueEmail(), "Tenant A");
        var teacherB = await RegisterTeacherWithPlatformAsync(client, UniqueEmail(), "Tenant B");
        var tokenA = await LoginAsync(client, teacherA.Email, Password, teacherA.Platform.PublicId);
        await CreateCourseAsync(client, teacherB.Platform.PublicId, teacherB.Platform.Slug,
            await LoginAsync(client, teacherB.Email, Password, teacherB.Platform.PublicId), "Foreign");

        var response = await GetAsync(
            client,
            $"/{teacherB.Platform.PublicId}/{teacherB.Platform.Slug}/api/platform/courses",
            tokenA);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Owner_MovesUnit_KeepsPositionsOrdered()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());
        var courseId = await CreateCourseAsync(client, publicId, slug, token, "Ordered");
        var unitA = await AddUnitAsync(client, publicId, slug, token, courseId, "A");
        var unitB = await AddUnitAsync(client, publicId, slug, token, courseId, "B");
        var unitC = await AddUnitAsync(client, publicId, slug, token, courseId, "C");

        var move = await PutJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}/units/{unitA.Id}",
            new UpdateUnitRequest(null, 3),
            token);
        move.StatusCode.Should().Be(HttpStatusCode.OK);

        var detail = await GetAsync(client, $"/{publicId}/{slug}/api/platform/courses/{courseId}", token);
        var body = await detail.Content.ReadFromJsonAsync<CourseDetailResponse>();
        body!.Units.Select(u => u.Id).Should().Equal(unitB.Id, unitC.Id, unitA.Id);
        body.Units.Select(u => u.Position).Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task Owner_UpdateUnitBlankTitle_Returns400()
    {
        using var client = NewClient();
        var (publicId, slug, token) = await NewOwnerClientAsync(client, UniqueEmail());
        var courseId = await CreateCourseAsync(client, publicId, slug, token, "Blank Title");
        var unit = await AddUnitAsync(client, publicId, slug, token, courseId, "Unit");

        var rename = await PutJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}/units/{unit.Id}",
            new UpdateUnitRequest("   ", null),
            token);

        rename.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static async Task<(string PublicId, string Slug, string Token)> NewOwnerClientAsync(HttpClient client, string email)
    {
        var teacher = await RegisterTeacherWithPlatformAsync(client, email, "Course Platform");
        var token = await LoginAsync(client, teacher.Email, Password, teacher.Platform.PublicId);
        return (teacher.Platform.PublicId, teacher.Platform.Slug, token);
    }

    private static async Task<Guid> CreateCourseAsync(HttpClient client, string publicId, string slug, string token, string title)
    {
        var response = await PostJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses",
            new CreateCourseRequest(title, null),
            token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var course = await response.Content.ReadFromJsonAsync<CourseResponse>();
        return course!.Id;
    }

    private static async Task<CourseUnitResponse> AddUnitAsync(HttpClient client, string publicId, string slug, string token, Guid courseId, string title)
    {
        var response = await PostJsonAsync(
            client,
            $"/{publicId}/{slug}/api/platform/courses/{courseId}/units",
            new AddUnitRequest(title, null),
            token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CourseUnitResponse>())!;
    }

    private static async Task<(Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform)> RegisterTeacherWithPlatformAsync(
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

    private static async Task<(Guid StudentId, string Email)> RegisterStudentAsync(HttpClient client, string email)
    {
        var register = await client.PostAsJsonAsync("/api/student/register",
            new RegisterStudentRequest("Student", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var student = await register.Content.ReadFromJsonAsync<RegisterStudentResponse>();
        return (student!.StudentId, email);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password, string publicId)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password, publicId));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>();
        return body!.Token;
    }

    private static async Task<string> LoginStudentAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/student/login", new StudentLoginRequest(email, password));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var body = await response.Content.ReadFromJsonAsync<StudentLoginResponse>();
        return body!.Token;
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string url, string token)
    {
        return await client.SendAsync(BuildRequest(HttpMethod.Get, url, token));
    }

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

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}