using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Purchases a Paid Published Course for a central Student using their tenant-scoped wallet balance
/// within a Teacher Platform. The purchase is executed as one atomic unit: it validates eligibility,
/// applies an optional single-use Student Coupon, debits the wallet by the final (discounted) amount,
/// records Purchase and CouponCredit FinancialTransactions, consumes the coupon, and creates the
/// Enrollment. A student cannot purchase a Free course here (Free courses use the direct-enrollment
/// flow), cannot purchase without sufficient balance, and cannot be charged twice while holding an
/// active enrollment.
/// </summary>
public sealed class PurchaseCourseService
{
    private readonly IPlatformRepository _platforms;
    private readonly IStudentRepository _students;
    private readonly ICourseRepository _courses;
    private readonly IStudentWalletRepository _wallets;
    private readonly IFinancialTransactionRepository _transactions;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IStudentCouponRepository _coupons;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public PurchaseCourseService(
        IPlatformRepository platforms,
        IStudentRepository students,
        ICourseRepository courses,
        IStudentWalletRepository wallets,
        IFinancialTransactionRepository transactions,
        IEnrollmentRepository enrollments,
        IStudentCouponRepository coupons,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _students = students;
        _courses = courses;
        _wallets = wallets;
        _transactions = transactions;
        _enrollments = enrollments;
        _coupons = coupons;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> PurchaseAsync(
        Guid studentId,
        string? teacherPublicId,
        Guid courseId,
        string? couponCode,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);

        if (platform.Status != PlatformStatus.Active)
        {
            throw new BusinessRuleViolationException("The teacher platform is not active.");
        }

        var currentTenant = _tenantContext.TenantId;
        var tenantScoped = false;

        try
        {
            if (!currentTenant.HasValue)
            {
                tenantScoped = _tenantContext.TrySetTenant(platform.Id);
            }

            var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
                ?? throw new NotFoundException("Course does not exist.");

            if (course.Status != CourseStatus.Published)
            {
                throw new BusinessRuleViolationException("Only published courses can be purchased.");
            }

            if (!course.IsPaid)
            {
                throw new BusinessRuleViolationException("Free courses use the direct enrollment flow.");
            }

            var existing = await _enrollments.GetActiveAsync(studentId, courseId, cancellationToken);

            if (existing is not null)
            {
                throw new BusinessRuleViolationException("The student already holds an active enrollment in this course.");
            }

            var price = course.Price!.Value;
            var coupon = string.IsNullOrWhiteSpace(couponCode)
                ? null
                : await ResolveCouponForPurchaseAsync(platform.Id, studentId, courseId, couponCode!, cancellationToken);

            var discount = coupon?.CalculateDiscount(price) ?? 0m;
            var finalAmount = coupon is null ? price : coupon.GetFinalAmount(price);

            var wallet = await GetOrCreateWalletAsync(studentId, platform.Id, cancellationToken);

            if (finalAmount > 0m && wallet.Balance < finalAmount)
            {
                throw new BusinessRuleViolationException("Insufficient wallet balance.");
            }

            FinancialTransaction? purchaseTransaction = null;
            FinancialTransaction? couponCreditTransaction = null;
            var balanceBeforeDebit = wallet.Balance;

            if (finalAmount > 0m)
            {
                try
                {
                    wallet.Debit(finalAmount);
                }
                catch (DomainException exception)
                {
                    throw new BusinessRuleViolationException(exception.Message, exception);
                }

                purchaseTransaction = new FinancialTransaction(
                    platform.Id,
                    wallet.Id,
                    studentId,
                    TransactionType.Purchase,
                    -finalAmount,
                    balanceBeforeDebit,
                    wallet.Balance,
                    courseId.ToString(),
                    studentId,
                    "student");
            }

            if (discount > 0m)
            {
                // CouponCredit is informational/audit only: it records the value covered by the coupon
                // without changing the wallet balance.
                couponCreditTransaction = new FinancialTransaction(
                    platform.Id,
                    wallet.Id,
                    studentId,
                    TransactionType.CouponCredit,
                    discount,
                    balanceBeforeDebit,
                    balanceBeforeDebit,
                    courseId.ToString(),
                    studentId,
                    "student");
            }

            if (coupon is not null)
            {
                var referenceTransactionId = (purchaseTransaction ?? couponCreditTransaction!)!.Id;
                try
                {
                    coupon.Consume(studentId, courseId, referenceTransactionId);
                }
                catch (DomainException exception)
                {
                    throw new BusinessRuleViolationException(exception.Message, exception);
                }
            }

            var enrollment = new Enrollment(studentId, courseId, platform.Id);

            _enrollments.Add(enrollment);

            if (purchaseTransaction is not null)
            {
                _transactions.Add(purchaseTransaction);
            }

            if (couponCreditTransaction is not null)
            {
                _transactions.Add(couponCreditTransaction);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return enrollment.Id;
        }
        finally
        {
            if (!currentTenant.HasValue && tenantScoped)
            {
                _tenantContext.Clear();
            }
        }
    }

    private async Task<StudentCoupon?> ResolveCouponForPurchaseAsync(
        Guid tenantId,
        Guid studentId,
        Guid courseId,
        string code,
        CancellationToken cancellationToken)
    {
        var coupon = await _coupons.GetByCodeForUpdateAsync(tenantId, code, cancellationToken)
            ?? throw new BusinessRuleViolationException("Coupon does not exist.");

        if (coupon.AssignedToStudentId != studentId)
        {
            throw new BusinessRuleViolationException("This coupon is assigned to a different student.");
        }

        if (coupon.CourseId != courseId)
        {
            throw new BusinessRuleViolationException("This coupon is not valid for the specified course.");
        }

        if (coupon.Status != CouponStatus.Active)
        {
            throw new BusinessRuleViolationException("Coupon is not active.");
        }

        if (DateTime.UtcNow > coupon.ExpiresAt)
        {
            throw new BusinessRuleViolationException("Coupon has expired.");
        }

        return coupon;
    }

    private async Task<StudentWallet> GetOrCreateWalletAsync(Guid studentId, Guid tenantId, CancellationToken cancellationToken)
    {
        var wallet = await _wallets.GetByStudentAndTenantAsync(studentId, tenantId, cancellationToken);

        if (wallet is not null)
        {
            return wallet;
        }

        wallet = new StudentWallet(studentId, tenantId);
        _wallets.Add(wallet);
        return wallet;
    }
}
