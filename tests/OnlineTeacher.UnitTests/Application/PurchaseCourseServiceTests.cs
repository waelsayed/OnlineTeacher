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
    private readonly FakeStudentCouponRepository _coupons = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private PurchaseCourseService CreateService() =>
        new(_platforms, _students, _courses, _wallets, _transactions, _enrollments, _coupons, _unitOfWork, _tenantContext);

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

    private StudentCoupon SeedActiveCoupon(
        TeacherPlatform platform,
        Student student,
        Course course,
        Guid teacherId,
        string code,
        DiscountType discountType,
        decimal discountValue,
        DateTime? expiresAt = null)
    {
        var coupon = new StudentCoupon(
            platform.Id,
            code,
            discountType,
            discountValue,
            expiresAt ?? DateTime.UtcNow.AddDays(30),
            course.Id,
            student.Id,
            teacherId);
        _coupons.Seed(coupon);
        return coupon;
    }

    [Fact]
    public async Task Purchase_PaidPublishedCourseAndEnoughBalance_DebitsWalletCreatesEnrollmentAndTransaction()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 150m);
        var wallet = SeedFundedWallet(student, platform, 200m);

        var id = await CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

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

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

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

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

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

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

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

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*published*");
    }

    [Fact]
    public async Task Purchase_UnknownCourse_ThrowsNotFound()
    {
        var (student, platform) = SeedEligibleTarget();
        SeedFundedWallet(student, platform, 200m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, Guid.NewGuid(), null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Purchase_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform);

        var act = () => CreateService().PurchaseAsync(Guid.NewGuid(), platform.PublicId.Value, course.Id, null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Purchase_InactivePlatform_ThrowsBusinessRuleViolation()
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create("AbCdEf123456"), Slug.CreateFromName("my-platform"));
        _students.Seed(student);
        _platforms.Seed(platform);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, Guid.NewGuid(), null);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*not active*");
    }

    [Fact]
    public async Task Purchase_InvalidPublicId_ThrowsValidation()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().PurchaseAsync(student.Id, "not-a-public-id", Guid.NewGuid(), null);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Purchase_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _) = SeedEligibleTarget();

        var act = () => CreateService().PurchaseAsync(student.Id, PublicId.Generate().Value, Guid.NewGuid(), null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Purchase_AlreadyActiveEnrollment_ThrowsBusinessRuleViolationNoDoubleDebit()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 300m);
        _enrollments.Seed(new Enrollment(student.Id, course.Id, platform.Id));

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

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

        var id = await CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

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

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, null);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task Purchase_PartialCoupon_DebitsFinalAmountRecordsPurchaseAndCouponCreditAndConsumes()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);
        var coupon = SeedActiveCoupon(platform, student, course, Guid.NewGuid(), "SAVE30",
            DiscountType.Percentage, 30m);

        var id = await CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "SAVE30");

        wallet.Balance.Should().Be(130m);
        coupon.Status.Should().Be(CouponStatus.Consumed);
        coupon.ConsumedAt.Should().NotBeNull();

        _transactions.Transactions.Should().HaveCount(2);
        var purchase = _transactions.Transactions.Should().ContainSingle(t => t.Type == TransactionType.Purchase).Subject;
        purchase.Amount.Should().Be(-70m);
        purchase.BalanceBefore.Should().Be(200m);
        purchase.BalanceAfter.Should().Be(130m);
        var credit = _transactions.Transactions.Should().ContainSingle(t => t.Type == TransactionType.CouponCredit).Subject;
        credit.Amount.Should().Be(30m);
        credit.BalanceBefore.Should().Be(200m);
        credit.BalanceAfter.Should().Be(200m);
        coupon.ConsumedInTransactionId.Should().Be(purchase.Id);

        _enrollments.Enrollments.Should().ContainSingle(e => e.Id == id);
    }

    [Fact]
    public async Task Purchase_FullDiscountCoupon_NoDebitRecordsCouponCreditOnlyAndConsumes()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 50m);
        var coupon = SeedActiveCoupon(platform, student, course, Guid.NewGuid(), "FREE100",
            DiscountType.Percentage, 100m);

        var id = await CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "FREE100");

        wallet.Balance.Should().Be(50m);
        coupon.Status.Should().Be(CouponStatus.Consumed);

        _transactions.Transactions.Should().ContainSingle();
        var credit = _transactions.Transactions[0];
        credit.Type.Should().Be(TransactionType.CouponCredit);
        credit.Amount.Should().Be(100m);
        coupon.ConsumedInTransactionId.Should().Be(credit.Id);

        _enrollments.Enrollments.Should().ContainSingle(e => e.Id == id);
    }

    [Fact]
    public async Task Purchase_FixedCouponAbovePrice_NoDebitAndConsumes()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 10m);
        var coupon = SeedActiveCoupon(platform, student, course, Guid.NewGuid(), "FIX150",
            DiscountType.Fixed, 150m);

        var id = await CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "FIX150");

        wallet.Balance.Should().Be(10m);
        coupon.Status.Should().Be(CouponStatus.Consumed);
        _transactions.Transactions.Should().ContainSingle(t => t.Type == TransactionType.CouponCredit);
        _enrollments.Enrollments.Should().ContainSingle(e => e.Id == id);
    }

    [Fact]
    public async Task Purchase_ExpiredCoupon_ThrowsAndNoSideEffects()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);
        var coupon = SeedActiveCoupon(platform, student, course, Guid.NewGuid(), "EXPR1",
            DiscountType.Percentage, 30m);
        var expiresAtProperty = typeof(StudentCoupon).GetProperty(nameof(StudentCoupon.ExpiresAt))!;
        expiresAtProperty!.SetValue(coupon, DateTime.UtcNow.AddMinutes(-1));

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "EXPR1");

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*expired*");
        wallet.Balance.Should().Be(200m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Purchase_ConsumedCoupon_ThrowsAndNoSideEffects()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);
        var coupon = SeedActiveCoupon(platform, student, course, Guid.NewGuid(), "CONS1",
            DiscountType.Percentage, 30m);
        coupon.Consume(student.Id, course.Id, Guid.NewGuid());

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "CONS1");

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*not active*");
        wallet.Balance.Should().Be(200m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Purchase_WrongCourseCoupon_ThrowsAndNoSideEffects()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var otherCourse = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);
        SeedActiveCoupon(platform, student, otherCourse, Guid.NewGuid(), "WRC1",
            DiscountType.Percentage, 30m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "WRC1");

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*not valid for the specified course*");
        wallet.Balance.Should().Be(200m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Purchase_WrongStudentCoupon_ThrowsAndNoSideEffects()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);
        var otherStudent = new Student("Omar", Email.Create("omar@example.com"));
        _students.Seed(otherStudent);
        SeedActiveCoupon(platform, otherStudent, course, Guid.NewGuid(), "WRS1",
            DiscountType.Percentage, 30m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "WRS1");

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*assigned to a different student*");
        wallet.Balance.Should().Be(200m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Purchase_UnknownCouponCode_ThrowsAndNoSideEffects()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "NOSUCH");

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*Coupon does not exist*");
        wallet.Balance.Should().Be(200m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Purchase_PartialCouponInsufficientRemainingBalance_ThrowsAndNoSideEffects()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 50m);
        SeedActiveCoupon(platform, student, course, Guid.NewGuid(), "PART50",
            DiscountType.Percentage, 30m);

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "PART50");

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*Insufficient wallet balance*");
        wallet.Balance.Should().Be(50m);
        _enrollments.Enrollments.Should().BeEmpty();
        _transactions.Transactions.Should().BeEmpty();
    }

    [Fact]
    public async Task Purchase_ConsumedCouponWithActiveEnrollment_ThrowsAndNoDoubleDebit()
    {
        var (student, platform) = SeedEligibleTarget();
        var course = SeedPaidPublishedCourse(platform, 100m);
        var wallet = SeedFundedWallet(student, platform, 200m);
        var coupon = SeedActiveCoupon(platform, student, course, Guid.NewGuid(), "DOUBLE1",
            DiscountType.Percentage, 30m);
        _enrollments.Seed(new Enrollment(student.Id, course.Id, platform.Id));

        var act = () => CreateService().PurchaseAsync(student.Id, platform.PublicId.Value, course.Id, "DOUBLE1");

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*active enrollment*");
        wallet.Balance.Should().Be(200m);
        coupon.Status.Should().Be(CouponStatus.Active);
        _transactions.Transactions.Should().BeEmpty();
    }
}
