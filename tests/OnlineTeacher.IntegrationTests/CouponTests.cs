using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using OnlineTeacher.Api.Contracts;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// End-to-end verification of Task 6 (Student Coupons) through the real HTTP API and a real
/// PostgreSQL Testcontainer: teacher coupon management (create/list/get/revoke), student course
/// purchase with a coupon (partial, full, fixed discount), coupon misuse (expired / consumed /
/// wrong-course / wrong-student), atomic rollback on failure, duplicate-code enforcement,
/// cross-tenant coupon isolation, and authorization (anonymous and permission enforcement).
/// </summary>
[Collection("api")]
public sealed class CouponTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private static int _counter;
    private readonly ApiFactory _factory;

    public CouponTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() =>
        $"coupon{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task Owner_CreatesListsGetsAndRevokesCoupon()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 200m);
        var (studentId, _) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var create = await PostJsonAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons",
            new CreateCouponRequest(
                "SAVE25",
                "Percentage",
                25m,
                DateTime.UtcNow.AddDays(30),
                courseId,
                studentId),
            teacher.Token);
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var couponId = (await create.Content.ReadFromJsonAsync<CreateCouponResponse>())!.CouponId;

        var list = await GetAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons",
            teacher.Token);
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        var coupons = await list.Content.ReadFromJsonAsync<List<CouponResponse>>();
        coupons.Should().ContainSingle(c => c.Id == couponId && c.Code == "SAVE25" && c.Status == "Active");

        var get = await GetAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons/{couponId}",
            teacher.Token);
        get.StatusCode.Should().Be(HttpStatusCode.OK);
        (await get.Content.ReadFromJsonAsync<CouponResponse>())!.Code.Should().Be("SAVE25");

        var revoke = await DeleteAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons/{couponId}",
            teacher.Token);
        revoke.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getAfter = await GetAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons/{couponId}",
            teacher.Token);
        (await getAfter.Content.ReadFromJsonAsync<CouponResponse>())!.Status.Should().Be("Expired");
    }

    [Fact]
    public async Task Student_PurchasesWithPartialCoupon_DebitsFinalAmountAndConsumes()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 200m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        var couponId = await CreateCouponAsync(client, teacher, "SAVE50", "Percentage", 50m, courseId, studentId);

        var purchase = await PostStudentJsonAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            new { couponCode = "SAVE50" },
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.Created, await purchase.Content.ReadAsStringAsync());

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(200m);
        detail.Transactions.Should().ContainSingle(t => t.Type == "Purchase" && t.Amount == -100m);
        detail.Transactions.Should().ContainSingle(t => t.Type == "CouponCredit" && t.Amount == 100m);

        var get = await GetAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons/{couponId}",
            teacher.Token);
        var coupon = await get.Content.ReadFromJsonAsync<CouponResponse>();
        coupon!.Status.Should().Be("Consumed");
        coupon.ConsumedInTransactionId.Should().NotBeNull();
    }

    [Fact]
    public async Task Student_PurchasesWithFullDiscountCoupon_NoDebitAndConsumes()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 200m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 10m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        await CreateCouponAsync(client, teacher, "FREE100", "Percentage", 100m, courseId, studentId);

        var purchase = await PostStudentJsonAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            new { couponCode = "FREE100" },
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.Created, await purchase.Content.ReadAsStringAsync());

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(10m);
        detail.Transactions.Should().ContainSingle(t => t.Type == "CouponCredit" && t.Amount == 200m);
        detail.Transactions.Should().NotContain(t => t.Type == "Purchase");
    }

    [Fact]
    public async Task Student_PurchasesWithFixedCouponApplyingReduction()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 300m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        await CreateCouponAsync(client, teacher, "FIX50", "Fixed", 50m, courseId, studentId);

        var purchase = await PostStudentJsonAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            new { couponCode = "FIX50" },
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.Created, await purchase.Content.ReadAsStringAsync());

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(50m);
        detail.Transactions.Should().ContainSingle(t => t.Type == "Purchase" && t.Amount == -250m);
    }

    [Fact]
    public async Task Student_ConsumedCouponCantBeUsedAgain_Returns422NoSideEffects()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 100m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        await CreateCouponAsync(client, teacher, "ONCE1", "Percentage", 30m, courseId, studentId);

        var first = await PostStudentJsonAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            new { couponCode = "ONCE1" },
            studentToken);
        first.StatusCode.Should().Be(HttpStatusCode.Created);

        var second = await PostStudentJsonAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            new { couponCode = "ONCE1" },
            studentToken);
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(230m, because: "one purchase at 70 after the 30 discount");
        detail.Transactions.Should().ContainSingle(t => t.Type == "Purchase");
    }

    [Fact]
    public async Task Student_WrongCourseCoupon_Returns422NoSideEffects()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseA = await CreatePaidCourseAsync(client, teacher, 100m);
        var courseB = await CreatePaidCourseAsync(client, teacher, 100m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseA);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseB);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        await CreateCouponAsync(client, teacher, "WRONG", "Percentage", 30m, courseA, studentId);

        var purchase = await PostStudentJsonAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseB}",
            new { couponCode = "WRONG" },
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var wallet = await GetAsync(
            client,
            $"/api/student/wallet/{teacher.Platform.PublicId}",
            studentToken);
        var detail = await wallet.Content.ReadFromJsonAsync<WalletDetailResponse>();
        detail!.Balance.Should().Be(300m);
        detail.Transactions.Should().NotContain(t => t.Type == "Purchase");
    }

    [Fact]
    public async Task Student_UnknownCouponCode_Returns422AndNoSideEffects()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 100m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (_, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        var purchase = await PostStudentJsonAsync(
            client,
            $"/api/student/purchase/{teacher.Platform.PublicId}/{courseId}",
            new { couponCode = "NOSUCH" },
            studentToken);
        purchase.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Owner_CreatingCouponForFreeCourse_Returns422()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreateFreeCourseAsync(client, teacher);
        var (studentId, _) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var create = await PostJsonAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons",
            new CreateCouponRequest(
                "FREE1",
                "Percentage",
                50m,
                DateTime.UtcNow.AddDays(30),
                courseId,
                studentId),
            teacher.Token);
        create.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Owner_CreatingDuplicateCouponCode_Returns422()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher);
        var (studentId, _) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        await CreateCouponAsync(client, teacher, "DUP1", "Percentage", 30m, courseId, studentId);

        var duplicate = await PostJsonAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons",
            new CreateCouponRequest(
                "dup1",
                "Percentage",
                40m,
                DateTime.UtcNow.AddDays(30),
                courseId,
                studentId),
            teacher.Token);
        duplicate.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Owner_CannotAccessAnotherTenantsCoupon_Returns404()
    {
        using var client = NewClient();
        var teacherA = await NewTeacherWithActivePlatformAsync(client);
        var teacherB = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacherA);
        var (studentId, _) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        var couponId = await CreateCouponAsync(client, teacherA, "TENNT", "Percentage", 30m, courseId, studentId);

        var listB = await GetAsync(
            client,
            $"/{teacherB.Platform.PublicId}/{teacherB.Platform.Slug}/api/platform/coupons",
            teacherB.Token);
        listB.StatusCode.Should().Be(HttpStatusCode.OK);
        (await listB.Content.ReadFromJsonAsync<List<CouponResponse>>()).Should().BeEmpty();

        var getB = await GetAsync(
            client,
            $"/{teacherB.Platform.PublicId}/{teacherB.Platform.Slug}/api/platform/coupons/{couponId}",
            teacherB.Token);
        getB.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var revokeB = await DeleteAsync(
            client,
            $"/{teacherB.Platform.PublicId}/{teacherB.Platform.Slug}/api/platform/coupons/{couponId}",
            teacherB.Token);
        revokeB.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Anonymous_CouponEndpoints_Returns401()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);

        var create = await client.PostAsJsonAsync(
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons",
            new CreateCouponRequest("X", "Percentage", 10m, DateTime.UtcNow.AddDays(30), Guid.NewGuid(), Guid.NewGuid()));
        create.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var list = await client.GetAsync(
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons");
        list.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var purchase = await client.PostAsync(
            $"/api/student/purchase/{teacher.Platform.PublicId}/{Guid.NewGuid()}",
            null);
        purchase.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AssistantWithoutCouponManage_CannotManageCoupons_Returns403()
    {
        using var client = NewClient();
        var owner = await NewTeacherWithActivePlatformAsync(client);
        var (studentId, _) = await RegisterAndLoginStudentAsync(client, UniqueEmail());

        var assistantEmail = UniqueEmail();
        await RegisterTeacherOnlyAsync(client, assistantEmail);
        var add = await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/members",
            new AddPlatformMemberRequest(assistantEmail),
            owner.Token);
        add.StatusCode.Should().Be(HttpStatusCode.OK, await add.Content.ReadAsStringAsync());
        var assistantToken = await LoginTeacherAsync(client, assistantEmail, Password, owner.Platform.PublicId);

        var create = await PostJsonAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/coupons",
            new CreateCouponRequest(
                "NOAUTH",
                "Percentage",
                10m,
                DateTime.UtcNow.AddDays(30),
                Guid.NewGuid(),
                studentId),
            assistantToken);
        create.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var list = await GetAsync(
            client,
            $"/{owner.Platform.PublicId}/{owner.Platform.Slug}/api/platform/coupons",
            assistantToken);
        list.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<Guid> CreateCouponAsync(
        HttpClient client,
        (Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token) teacher,
        string code,
        string discountType,
        decimal discountValue,
        Guid courseId,
        Guid studentId)
    {
        var response = await PostJsonAsync(
            client,
            $"/{teacher.Platform.PublicId}/{teacher.Platform.Slug}/api/platform/coupons",
            new CreateCouponRequest(code, discountType, discountValue, DateTime.UtcNow.AddDays(30), courseId, studentId),
            teacher.Token);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CreateCouponResponse>())!.CouponId;
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
            new CreateTeacherPlatformRequest(teacher!.TeacherId, "Coupon Platform"));
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
        HttpClient client, (Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token) teacher, decimal price = 100m)
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

    private static async Task<HttpResponseMessage> DeleteAsync(HttpClient client, string url, string token) =>
        await client.SendAsync(BuildRequest(HttpMethod.Delete, url, token));

    private static async Task<HttpResponseMessage> PostJsonAsync<T>(HttpClient client, string url, T body, string token)
        where T : class
    {
        var request = BuildRequest(HttpMethod.Post, url, token);
        request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }

    private static async Task<HttpResponseMessage> PostStudentJsonAsync<T>(HttpClient client, string url, T body, string token)
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

internal sealed record CreateCouponResponse(Guid CouponId);
