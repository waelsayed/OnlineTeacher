using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class PurchaseCourseServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeStudentWalletRepository _wallets = new();
    private readonly FakeFinancialTransactionRepository _transactions = new();
    private readonly FakeEnrollmentRepository _enrollments = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private PurchaseCourseService CreateService() =>
        new(_platforms, _students, _courses, _wallets, _transactions, _enrollments, _unitOfWork, _tenantContext);

    private (Student Student, TeacherPlatform Platform) SeedEligibleTarget(string publicId = "AbCdEf123456")
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        platform.Activate();
        _students.Seed(student);
        _platforms.Seed(platform);
        return (student, platform);
    }

    private Course SeedPaidPublishedCourse(TeacherPlatform platform, decimal price = 100m)
    {
        var course = new Course(platform.Id, "Algebra", "An algebra course.", CoursePricingType.Paid, price);
        course.Publish();
        _courses.Seed(course);
        return course;
    }

    private StudentWallet SeedFundedWallet(Student student, TeacherPlatform platform, decimal balance)
    {
        var wallet = new StudentWallet(student.Id, platform.Id);
        wallet.Credit(balance);
        _wallets.Seed(wallet);
        return wallet;
    }

    [Fact]
    public async Task Purchase_PaidPublishedCourseAndEnoughBalance_DebitsWalletCreatesEnrollmentAndTransaction()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 150m);
        var wallet = SeedFundedWallet(student, platform, 200m);

        var id = await CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        var enrollment = _enrollments.Enrollments.Should().ContainSingle().Subject;
        enrollment.Id.Should().Be(id);
        enrollment.StudentId.Should().Be(student.Id);
        enrollment.CourseId.Should().Be(course.Id);
        enrollment.TenantId.Should().Be(platform.Id);
        enrollment.Status.Should().Be(EnrollmentStatus.Active);

        wallet.Balance.Should().Be(50m);

        var transaction = _transactions.Transactions.Should().ContainSingle().Subject;
        transaction.Type.Should().Be(TransactionType.Purchase);
        transaction.Amount.Should().Be(-150m);
        transaction.BalanceBefore.Should().Be(200m);
        transaction.BalanceAfter.Should().Be(50m);
        transaction.Reference.Should().Be(course.Id.ToString());
        transaction.ActorId.Should().Be(student.Id);
        transaction.ActorType.Should().Be("student");

        _unitOfWork.SaveCount.Should().Be(1);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Purchase_NoExistingWalletAndPositivePrice_ThrowsInsufficientBalance()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*Insufficient wallet balance*");

        _wallets.Wallets.Should().ContainSingle();
        _wallets.Wallets[0].Balance.Should().Be(0m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Purchase_InsufficientBalance_ThrowsBusinessRuleViolationAndNoSideEffects()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 200m);
        var wallet = SeedFundedWallet(student, platform, 100m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*Insufficient wallet balance*");

        wallet.Balance.Should().Be(100m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Purchase_FreeCourse_ThrowsBusinessRuleViolation()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = new Course(platform.Id, "Free Course", null, CoursePricingType.Free);
        course.Publish();
        _courses.Seed(course);
        SeedFundedWallet(student, platform, 100m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*direct enrollment*");
    }

    [Fact]
    public async Task Purchase_UnpublishedCourse_ThrowsBusinessRuleViolation()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = new Course(platform.Id, "Draft", null, CoursePricingType.Paid, 100m);
        _courses.Seed(course);
        SeedFundedWallet(student, platform, 200m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*published*");
    }

    [Fact]
    public async Task Purchase_UnknownCourse_ThrowsNotFound()
    {
        var (student, platform) = SeedEligibleTarget();
        SeedFundedWallet(student, platform, 200m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Purchase_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform);

        var act = () => CreateService().PurchaseAsync(Guid.NewGuid(), platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Purchase_InactivePlatform_ThrowsBusinessRuleViolation()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create("AbCdEf123456"), Slug.CreateFromName("my-platform"));
        _students.Seed(student);
        _platforms.Seed(platform);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Purchase_InvalidPublicId_ThrowsValidation()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().PurchaseAsync(student.Id, "not-a-public-id", Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Purchase_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().PurchaseAsync(student.Id, PublicId.Generate().Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Purchase_AlreadyActiveEnrollment_ThrowsBusinessRuleViolationNoDoubleDebit()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 300m);
        _enrollments.Seed(new Enrollment(student.Id, course.Id, platform.Id));

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*active enrollment*");

        wallet.Balance.Should().Be(300m);
        _transactions.Transactions.Should().BeEmpty();
        _enrollments.Enrollments.Should().ContainSingle();
    }

    [Fact]
    public async Task Purchase_AfterCancelledEnrollment_AllowsNewPurchaseAndDebits()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);
        var previous = new Enrollment(student.Id, course.Id, platform.Id);
        previous.Cancel();
        _enrollments.Seed(previous);

        var id = await CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        _enrollments.Enrollments.Should().HaveCount(2);
        _enrollments.Enrollments.Should().Contain(e => e.Id == id && e.Status == EnrollmentStatus.Active);
        _enrollments.Enrollments.Should().Contain(e => e.Id == previous.Id && e.Status == EnrollmentStatus.Cancelled);
        wallet.Balance.Should().Be(100m);
        _transactions.Transactions.Should().ContainSingle();
    }

    [Fact]
    public async Task Purchase_UnderTenantContext_ThrowsTenantMismatch()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform);
        _tenantContext.TrySetTenant(platform.Id);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}
