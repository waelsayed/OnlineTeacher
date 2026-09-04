using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Infrastructure.Persistence;
using OnlineTeacher.Infrastructure.Tenancy;
using Xunit;

namespace OnlineTeacher.IntegrationTests;

/// <summary>
/// Real concurrency integration test for coupon consumption. Exercises two genuinely concurrent
/// purchase attempts against the SAME tenant, student, course, and coupon using separate database
/// connections to PostgreSQL Testcontainer. Verifies that the SELECT ... FOR UPDATE pessimistic
/// lock inside the explicit transaction prevents double consumption.
/// </summary>
[Collection("api")]
public sealed class ConcurrentCouponPurchaseTests
{
    private const string Password = "correct-horse-battery-staple-123";
    private static int _counter;
    private readonly ApiFactory _factory;

    public ConcurrentCouponPurchaseTests(ApiFactory factory)
    {
        _factory = factory;
    }

    private static string UniqueEmail() =>
        $"concurrency{Interlocked.Increment(ref _counter)}-{Guid.NewGuid():N}@example.com";

    private HttpClient NewClient() =>
        _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    [Fact]
    public async Task TwoConcurrentPurchaseAttemptsWithSameCoupon_ExactlyOneSucceeds()
    {
        using var client = NewClient();
        var teacher = await NewTeacherWithActivePlatformAsync(client);
        var courseId = await CreatePaidCourseAsync(client, teacher, 200m);
        await PublishCourseAsync(client, teacher.Platform.PublicId, teacher.Platform.Slug, teacher.Token, courseId);
        var (studentId, studentToken) = await RegisterAndLoginStudentAsync(client, UniqueEmail());
        await FundWalletAsync(client, teacher.Platform.PublicId, studentToken, 300m);
        await ApproveAllTransfersAsync(client, teacher, studentToken);

        await CreateCouponAsync(client, teacher, "RACE", "Percentage", 50m, courseId, studentId);

        // Extract connection string from the Testcontainer via the factory configuration.
        var config = _factory.Services.GetRequiredService<IConfiguration>();
        var connectionString = config["ConnectionStrings:DefaultConnection"]!;

        // Build two separate DbContext instances sharing the same database but with independent
        // connections (each DbContext gets its own pooled connection). Each starts in a central
        // (null) tenant scope, matching the real API request; PurchaseCourseService then switches
        // to the platform tenant internally (via ITenantContext.TrySetTenant) once the platform is
        // resolved, so EF query filters are correct inside the transaction.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(
                maxRetryCount: 1,
                maxRetryDelay: TimeSpan.FromSeconds(1),
                errorCodesToAdd: null))
            .Options;

        var tenantId = teacher.Platform.PlatformId;

        var svc1 = CreateScopedService(options, tenantId);
        var svc2 = CreateScopedService(options, tenantId);

        try
        {
            // Fire two genuinely concurrent purchase attempts against the same coupon. Each
            // service runs on its own DbContext and database connection, so the two attempts
            // genuinely contend at the PostgreSQL row level.
            var task1 = svc1.Service.PurchaseAsync(studentId, teacher.Platform.PublicId, courseId, "RACE");
            var task2 = svc2.Service.PurchaseAsync(studentId, teacher.Platform.PublicId, courseId, "RACE");

            var attempts = new List<Task<Guid>> { task1, task2 };

            // Wait until the tasks settle (one succeeds, the other must fail on the consumed coupon).
            await Task.WhenAll(
                attempts.Select(t => t.ContinueWith(_ => { })));

            int successCount = attempts.Count(t => t.Status == TaskStatus.RanToCompletion);
            int failureCount = attempts.Count(t => t.Status == TaskStatus.Faulted);

            var faultMessages = attempts
                .Where(t => t.Status == TaskStatus.Faulted)
                .SelectMany(t => t.Exception!.InnerExceptions.Select(e => $"{e.GetType().Name}: {e.Message}"))
                .ToList();

            // The second attempt must fail because the coupon is no longer available/consumable.
            failureCount.Should().BeGreaterThanOrEqualTo(1,
                because: "the FOR UPDATE pessimistic lock must serialize the two attempts and the second must observe the consumed coupon");
            successCount.Should().Be(1,
                because: "exactly one purchase may succeed. Faults: " + string.Join(" | ", faultMessages));

            // Any faulted task must have failed with a business rule violation, not a database error.
            foreach (var faulted in attempts.Where(t => t.Status == TaskStatus.Faulted))
            {
                faulted.Exception!.InnerExceptions.Should().Contain(e => e is BusinessRuleViolationException,
                    because: "the losing purchase must fail cleanly with a business rule violation");
            }

            // Verify database state using a fresh read-only context.
            await using var verifyDb = new ApplicationDbContext(options, CreateTenantContext(tenantId));
            var enrollments = await verifyDb.Enrollments
                .Where(e => e.StudentId == studentId && e.CourseId == courseId)
                .ToListAsync();

            enrollments.Should().ContainSingle(
                because: "exactly one enrollment must exist despite two concurrent purchase attempts");

            var wallet = await verifyDb.StudentWallets
                .FirstOrDefaultAsync(w => w.StudentId == studentId && w.TenantId == tenantId);

            wallet.Should().NotBeNull();
            wallet!.Balance.Should().Be(200m,
                because: "initial 300 minus one 100 payment after 50% coupon discount on 200 course");

            var purchaseTransactions = await verifyDb.FinancialTransactions
                .Where(ft => ft.WalletId == wallet.Id && ft.Type == TransactionType.Purchase)
                .ToListAsync();

            purchaseTransactions.Should().ContainSingle(
                because: "exactly one Purchase debit must exist");

            var coupon = await verifyDb.StudentCoupons
                .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Code == "RACE");

            coupon.Should().NotBeNull();
            coupon!.Status.Should().Be(CouponStatus.Consumed,
                because: "the consumed coupon must be marked Consumed");
            coupon.ConsumedAt.Should().NotBeNull();
            coupon.ConsumedInTransactionId.Should().NotBeNull();
        }
        finally
        {
            await svc1.Db.DisposeAsync();
            await svc2.Db.DisposeAsync();
        }
    }

    private static (PurchaseCourseService Service, ApplicationDbContext Db) CreateScopedService(
        DbContextOptions<ApplicationDbContext> options, Guid tenantId)
    {
        // The service starts in a CENTRAL (null) tenant scope, matching the real API request
        // scope. PurchaseCourseService internally switches to the platform tenant (TrySetTenant)
        // after resolving the platform, then clears it on exit.
        var tenantContext = new TenantContext();
        var db = new ApplicationDbContext(options, tenantContext);
        var platforms = new PlatformRepository(db);
        var students = new StudentRepository(db);
        var courses = new CourseRepository(db);
        var wallets = new StudentWalletRepository(db);
        var transactions = new FinancialTransactionRepository(db);
        var enrollments = new EnrollmentRepository(db);
        var coupons = new StudentCouponRepository(db);
        var unitOfWork = new EfUnitOfWork(db);

        var service = new PurchaseCourseService(
            platforms, students, courses, wallets, transactions,
            enrollments, coupons, unitOfWork, tenantContext);

        return (service, db);
    }

    private static TenantContext CreateTenantContext(Guid tenantId)
    {
        var ctx = new TenantContext();
        ctx.TrySetTenant(tenantId);
        return ctx;
    }

    private static async Task<(Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token)>
        NewTeacherWithActivePlatformAsync(HttpClient client)
    {
        var email = UniqueEmail();
        var register = await client.PostAsJsonAsync("/api/central/teachers/register",
            new RegisterTeacherRequest("Teacher", email, Password));
        register.StatusCode.Should().Be(HttpStatusCode.Created, await register.Content.ReadAsStringAsync());
        var teacher = await register.Content.ReadFromJsonAsync<RegisterTeacherResponse>();

        var create = await client.PostAsJsonAsync("/api/central/platforms",
            new CreateTeacherPlatformRequest(teacher!.TeacherId, "Concurrency Platform"));
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var platform = await create.Content.ReadFromJsonAsync<CreateTeacherPlatformResponse>();

        var token = await LoginTeacherAsync(client, email, Password, platform!.PublicId);
        await ActivatePlatformAsync(client, platform.PublicId, token);

        return (teacher.TeacherId, email, platform, token);
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
        HttpClient client,
        (Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token) teacher,
        decimal price)
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

    private static async Task FundWalletAsync(HttpClient client, string publicId, string studentToken, decimal amount)
    {
        var submit = await PostJsonAsync(
            client,
            $"/api/student/wallet/{publicId}/transfer",
            new SubmitTransferRequest(amount, "VodafoneCash", "FUND-" + Guid.NewGuid().ToString("N")),
            studentToken);
        submit.StatusCode.Should().Be(HttpStatusCode.Created, await submit.Content.ReadAsStringAsync());
    }

    private static async Task ApproveAllTransfersAsync(
        HttpClient client,
        (Guid TeacherId, string Email, CreateTeacherPlatformResponse Platform, string Token) teacher,
        string studentToken)
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

    private static async Task CreateCouponAsync(
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

    private static HttpRequestMessage BuildRequest(HttpMethod method, string url, string token)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }
}
