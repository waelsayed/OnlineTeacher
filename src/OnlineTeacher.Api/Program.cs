using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Middleware;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Security;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Infrastructure.Persistence;
using OnlineTeacher.Infrastructure.Security;
using OnlineTeacher.Infrastructure.Tenancy;

var builder = WebApplication.CreateBuilder(args);

// JSON console structured logging (built-in), environment-aware.
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
    options.UseUtcTimestamp = true;
});

builder.Services.AddControllers();

// Options
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

// Infrastructure / composition root
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = OnlineTeacher.Infrastructure.Persistence.ConnectionFactory.Build();
}

builder.Services
    .AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString, npgsql => npgsql.EnableRetryOnFailure(
            maxRetryCount: 1,
            maxRetryDelay: TimeSpan.FromSeconds(1),
            errorCodesToAdd: null)))
    .AddScoped<PermissionSeeder>();

builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<IPasswordHasher>(_ => new PasswordHasherService());

builder.Services.AddScoped<ITeacherRepository, TeacherRepository>();
builder.Services.AddScoped<IPlatformRepository, PlatformRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<ITeacherPlatformAccessRepository, TeacherPlatformAccessRepository>();
builder.Services.AddScoped<IPlatformMembershipRepository, PlatformMembershipRepository>();
builder.Services.AddScoped<IStudentRepository, StudentRepository>();
builder.Services.AddScoped<IStudentFollowRepository, StudentFollowRepository>();
builder.Services.AddScoped<ICourseRepository, CourseRepository>();
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
builder.Services.AddScoped<IStudentWalletRepository, StudentWalletRepository>();
builder.Services.AddScoped<IFinancialTransactionRepository, FinancialTransactionRepository>();
builder.Services.AddScoped<ITransferRequestRepository, TransferRequestRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

// Application use cases
builder.Services.AddScoped<RegisterTeacherService>();
builder.Services.AddScoped<CreateTeacherPlatformService>();
builder.Services.AddScoped<ActivateTeacherPlatformService>();
builder.Services.AddScoped<AuthenticateTeacherService>();
builder.Services.AddScoped<TenantRouteResolver>();
builder.Services.AddScoped<GetTeacherPlatformAccessService>();
builder.Services.AddScoped<GetPlatformProfileService>();
builder.Services.AddScoped<UpdatePlatformProfileService>();
builder.Services.AddScoped<ListPlatformMembersService>();
builder.Services.AddScoped<AddPlatformMemberService>();
builder.Services.AddScoped<ChangePlatformMemberRoleService>();
builder.Services.AddScoped<RemovePlatformMemberService>();
builder.Services.AddScoped<RegisterStudentService>();
builder.Services.AddScoped<AuthenticateStudentService>();
builder.Services.AddScoped<GetStudentProfileService>();
builder.Services.AddScoped<FollowTeacherService>();
builder.Services.AddScoped<UnfollowTeacherService>();
builder.Services.AddScoped<ListFollowedTeachersService>();
builder.Services.AddScoped<IsFollowingTeacherService>();
builder.Services.AddScoped<CreateCourseService>();
builder.Services.AddScoped<UpdateCourseService>();
builder.Services.AddScoped<GetCourseService>();
builder.Services.AddScoped<ListCoursesService>();
builder.Services.AddScoped<DeleteCourseService>();
builder.Services.AddScoped<AddUnitService>();
builder.Services.AddScoped<UpdateUnitService>();
builder.Services.AddScoped<RemoveUnitService>();
builder.Services.AddScoped<AddLessonService>();
builder.Services.AddScoped<UpdateLessonService>();
builder.Services.AddScoped<RemoveLessonService>();
builder.Services.AddScoped<EnrollStudentService>();
builder.Services.AddScoped<ListStudentEnrollmentsService>();
builder.Services.AddScoped<CancelEnrollmentService>();
builder.Services.AddScoped<ListCourseEnrollmentsService>();
builder.Services.AddScoped<SubmitTransferRequestService>();
builder.Services.AddScoped<ReviewTransferRequestService>();
builder.Services.AddScoped<ListTransferRequestsService>();
builder.Services.AddScoped<ListStudentWalletService>();
builder.Services.AddScoped<PurchaseCourseService>();

// API-framework services
builder.Services.AddScoped<JwtTokenFactory>();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, PrincipalTypeHandler>();
builder.Services.AddTransient<ExceptionHandlingMiddleware>();
builder.Services.AddTransient<TenantRouteMiddleware>();

// JWT Bearer authentication. Routering and validation read the SAME resolved
// JwtOptions (via the options DI pipeline), so the token issued by JwtTokenFactory and the
// parameters the bearer middleware validates with are always consistent — including when a
// host (e.g. WebApplicationFactory) overrides Jwt configuration.
JwtSecurityTokenHandler.DefaultMapInboundClaims = false;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((jwtBearer, jwtOptionsAccessor) =>
    {
        var jwt = jwtOptionsAccessor.Value;
        if (string.IsNullOrWhiteSpace(jwt.SigningKey))
        {
            throw new InvalidOperationException("JWT signing key is not configured.");
        }

        jwtBearer.RequireHttpsMetadata = false;
        jwtBearer.SaveToken = true;
        jwtBearer.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey))
            {
                KeyId = jwt.KeyId
            },
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseMiddleware<TenantRouteMiddleware>();
app.UseAuthorization();

app.MapControllers();

// Lightweight liveness probe for container orchestration (e.g. Docker Compose dependency).
app.MapHealthChecks("/health");

// Apply migrations and seed the permission catalog deterministically at startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<PermissionSeeder>();
    await seeder.SeedAsync();
}

await app.RunAsync();

public partial class Program
{
}