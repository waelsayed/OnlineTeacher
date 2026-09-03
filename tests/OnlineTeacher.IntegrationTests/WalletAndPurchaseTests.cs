using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineTeacher.Api.Contracts;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// End-to-end verification of Task 5 (Student Wallet & Course Purchase) through the real HTTP API
/// and a real PostgreSQL Testcontainer: wallet credit via transfer submit/approve/reject, paid-course
/// purchase (atomic balance debit + enrollment), insufficient-balance and duplicate/atomically-guarded
/// failures, free-course direct enrollment preservation, re-purchase after terminal cancellation, and
/// tenant-isolation / authorization for the wallet and purchase endpoints.
/// </summary>
[Collection("api")]
public sealed class WalletAndPurchaseTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private static int _counter;
    private readonly ApiFactory _factory;

    public WalletAndPurchaseTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() =>
        $"wallet{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Student_SubmitsTransferAndOwnerApproves_WalletCredited()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var submit = await PostJsonAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}/transfer",
            new SubmitTransferRequest(200m, "VodafoneCash", "REF-001"),
            studentToken);
        submit.StatusCode.Should().Be(HttpStatusCode.Created, await submit.Content.ReadAsStringAsync());
        var transferId = (await submit.Content.ReadFromJsonAsync<TransferSubmitResponse>())!.TransferId;

        var approve = await PostAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers/{transferId}/approve",
            teacher.Token);
        approve.StatusCode.Should().Be(HttpStatusCode.OK, await approve.Content.ReadAsStringAsync());

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        wallet.StatusCode.Should().Be(HttpStatusCode.OK);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(200m);
        detail.Currency.Should().Be("EGP");
        detail.Transactions.Should().ContainSingle(t => t.Type == "Credit" && t.Amount == 200m);
    }

    [Fact]
    public async Task Owner_ApproveSameTransferTwice_Returns422AndCreditedOnce()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var submit = await PostJsonAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}/transfer",
            new SubmitTransferRequest(150m, "InstaPay", "REF-002"),
            studentToken);
        submit.StatusCode.Should().Be(HttpStatusCode.Created);
        var transferId = (await submit.Content.ReadFromJsonAsync<TransferSubmitResponse>())!.TransferId;

        var first = await PostAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers/{transferId}/approve",
            teacher.Token);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await PostAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers/{transferId}/approve",
            teacher.Token);
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(150m);
        detail.Transactions.Should().ContainSingle(t => t.Type == "Credit");
    }

    [Fact]
    public async Task Owner_RejectTransfer_WalletNotCredited()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var submit = await PostJsonAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}/transfer",
            new SubmitTransferRequest(100m, "VodafoneCash", "REF-003"),
            studentToken);
        submit.StatusCode.Should().Be(HttpStatusCode.Created);
        var transferId = (await submit.Content.ReadFromJsonAsync<TransferSubmitResponse>())!.TransferId;

        var reject = await PostAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers/{transferId}/reject",
            teacher.Token);
        reject.StatusCode.Should().Be(HttpStatusCode.OK);

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(0m);
        detail.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Student_FundsWalletAndPurchasesPaidCourse_CreatesEnrollmentAndDebits()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 200m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);

        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        var purchase = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.Created, await purchase.Content.ReadAsStringAsync());
        (await purchase.Content.ReadFromJsonAsync<PurchaseResponse>())!.EnrollmentId.Should().NotBeEmpty();

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(100m);
        detail.Transactions.Should().ContainSingle(t => t.Type == "Purchase" && t.Amount == -200m);

        var enrollments = await GetStudentAsync(
            client,
            $"/api/student/enrollments/{teacher.Platform.PublicId}",
            studentToken);
        var list = await enrollments.Content.ReadFromJsonAsync<List<EnrollmentListItemResponse>>();
        list.Should().ContainSingle(e => e.CourseId == courseId && e.Status == "Active");
    }

    [Fact]
    public async Task Student_PurchasesWithInsufficientBalance_Returns422NoSideEffects()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 500m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);

        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 100m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        var purchase = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(100m);
        detail.Transactions.Should().NotContain(t => t.Type == "Purchase");

        var enrollments = await GetStudentAsync(
            client,
            $"/api/student/enrollments/{teacher.Platform.PublicId}",
            studentToken);
        var list = await enrollments.Content.ReadFromJsonAsync<List<EnrollmentListItemResponse>>();
        list.Should().NotContain(e => e.CourseId == courseId);
    }

    [Fact]
    public async Task Student_PurchasesPublishedPaidCourse_IsRequired()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 100m);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 200m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        // Course is Draft (not published): purchase must fail.
        var purchase = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Student_CannotPurchaseFreeCourseThroughPurchaseEndpoint()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreateFreeCourseAsync(client, teacher);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 100m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        var purchase = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // Free course still enrolls via the direct-enroll flow.
        var enroll = await PostStudentAsync(
            client,
            $"/api/student/enroll/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        enroll.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Student_DuplicateActivePurchase_Returns422NoDoubleDebit()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 100m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        var first = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(200m);
        detail.Transactions.Should().ContainSingle(t => t.Type == "Purchase");
    }

    [Fact]
    public async Task Student_RepurchasesAfterTerminalCancellation_Permitted()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 100m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 500m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        var first = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var cancel = await DeleteStudentAsync(
            client,
            $"/api/student/enrollments/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var repurchase = await PostStudentAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            studentToken);
        repurchase.StatusCode.Should().Be(HttpStatusCode.Created, await repurchase.Content.ReadAsStringAsync());

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(300m);
        detail.Transactions.Should().HaveCount(3, because: "one credit plus two purchase debits");
    }

    [Fact]
    public async Task Owner_CannotReviewAnotherTenantsTransfer_Returns404()
    {
        using var client = NewClient();
        var teacherA = await NewTeacherWithActivePlatformAsync(client);
        var teacherB = await NewTeacherWithActivePlatformAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var submit = await PostJsonAsync(
            client,
            $"/api/student/wallet/{teacherA.Platform.PublicId}/transfer",
            new SubmitTransferRequest(100m, "VodafoneCash", "REF-010"),
            studentToken);
        submit.StatusCode.Should().Be(HttpStatusCode.Created);
        var transferId = (await submit.Content.ReadFromJsonAsync<TransferSubmitResponse>())!.TransferId;

        var approve = await PostAsync(
            client,
            $"/{teacherB.Platform.PublicId}/{teacherB.Platform.Slug}/api/platform/wallet/transfers/{transferId}/approve",
            teacherB.Token);
        approve.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_TransferAndPurchase_Returns401()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);

        var submit = await client.PostAsJsonAsync(
            $"/api/student/wallet/{teacher.Platform.PublicId}/transfer",
            new SubmitTransferRequest(100m, "VodafoneCash", null));
        submit.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var purchase = await client.PostAsync(
            $"/api/student/purchase/{teacher.Platform.PublicId}/{Guid.NewGuid()}",
            null);
        purchase.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var list = await client.GetAsync(
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers");
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssistantWithoutWalletManage_CannotReviewTransfers_Returns403()
    {
        using var client = NewClient();
        var owner = await NewTeacherWithActivePlatformAsync(client);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var submit = await PostJsonAsync(
            client,
            $"/api/student/wallet/{owner.Platform.PublicId}/transfer",
            new SubmitTransferRequest(100m, "VodafoneCash", "REF-011"),
            studentToken);
        submit.StatusCode.Should().Be(HttpStatusCode.Created);
        var transferId = (await submit.Content.ReadFromJsonAsync<TransferSubmitResponse>())!.TransferId;

        var assistantEmail = UniqueEmail();
        var assistantTeacher = await RegisterTeacherOnlyAsync(client, assistantEmail);
        var add = await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistantEmail),
            owner.Token);
        add.StatusCode.Should().Be(HttpStatusCode.OK, await add.Content.ReadAsStringAsync());
        var assistantToken = await LoginTeacherAsync(client, assistantEmail, Password, owner.Platform.PublicId);

        // Assistant is a member but has no Wallet.Manage permission.
        var approve = await PostAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/wallet/transfers/{transferId}/approve",
            assistantToken);
        approve.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Student_CannotAccessAnotherStudentsWallet()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var (studentA, tokenA) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var (_, tokenB) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var submitA = await PostJsonAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}/transfer",
            new SubmitTransferRequest(100m, "VodafoneCash", "REF-012"),
            tokenA);
        submitA.StatusCode.Should().Be(HttpStatusCode.Created);
        var transferId = (await submitA.Content.ReadFromJsonAsync<TransferSubmitResponse>())!.TransferId;
        await PostAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers/{transferId}/approve",
            teacher.Token);

        // Student B queries the same platform wallet: it resolves to Student B's own (empty) wallet.
        // An empty, not-yet-created wallet is returned as 204 No Content with no body.
        var walletB = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            tokenB);
        walletB.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private static async Task FundWalletAsync(
        HttpClient client, string publicId, string studentToken, decimal amount)
    {
        var submit = await PostJsonAsync(
            client,
            $"/api/student/wallet/{publicId}/transfer",
            new SubmitTransferRequest(amount, "VodafoneCash", "FUND-" + Guid.NewGuid().ToString("N")),
            studentToken);
        submit.StatusCode.Should().Be(HttpStatusCode.Created, await submit.Content.ReadAsStringAsync());
    }

    private static async Task ApproveAllTransfersAsync(
        HttpClient client, (Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token) teacher, string studentToken)
    {
        var list = await GetAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers",
            teacher.Token);
        list.StatusCode.Should().Be(HttpStatusCode.OK, await list.Content.ReadAsStringAsync());
        var transfers = await list.Content.ReadFromJsonAsync<List<WalletTransferRequestResponse>>();
        foreach (var transfer in transfers!)
        {
            var approve = await PostAsync(
                client,
                $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/wallet/transfers/{transfer.RequestId}/approve",
                teacher.Token);
            approve.StatusCode.Should().Be(HttpStatusCode.OK, await approve.Content.ReadAsStringAsync());
        }
    }

    private static async Task<(Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token)> NewTeacherWithActivePlatformAsync(
        HttpClient client)
    {
        var email = UniqueEmail();
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Teacher", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();

        var create = await client.PostAsJsonAsync("/api/central/platforms",
            new CreateTeacherPlatformRequest(teacher!.TeacherId, "Wallet Platform"));
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var platform = await create.Content.ReadFromJsonAsync<CreateTeacherPlatformResponse>();

        var token = await LoginTeacherAsync(client, email, Password, platform!.PublicId);
        await ActivatePlatformAsync(client, platform.PublicId, token);

        return (teacher.TeacherId, email, platform, token);
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

    private static async Task<Guid> CreatePaidCourseAsync(
        HttpClient client, (Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token) teacher, decimal price)
    {
        var response = await PostJsonAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/courses",
            new CreateCourseRequest("Paid Course", null, "Paid", price),
            teacher.Token);
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        var course = await response.Content.ReadFromJsonAsync<CourseResponse>();
        return course!.Id;
    }

    private static async Task<Guid> CreateFreeCourseAsync(
        HttpClient client, (Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token) teacher)
    {
        var response = await PostJsonAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/courses",
            new CreateCourseRequest("Free Course", null),
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

    private static async Task<HttpResponseMessage> GetStudentAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Get, url, token));

    private static async Task<HttpResponseMessage> PostStudentAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Post, url, token));

    private static async Task<HttpResponseMessage> DeleteStudentAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Delete, url, token));

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

internal sealed record TransferSubmitResponse(Guid TransferId);
internal sealed record PurchaseResponse(Guid EnrollmentId);
