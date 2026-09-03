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
/// debits the wallet, records a Purchase FinancialTransaction, and creates the Enrollment. A student
/// cannot purchase a Free course here (Free courses use the direct-enrollment flow), cannot purchase
/// without sufficient balance, and cannot be charged twice while holding an active enrollment. A new
/// purchase is permitted after a previous enrollment reached its terminal (cancelled) state while
/// preserving the prior history.
/// </summary>
public sealed class PurchaseCourseService
{
    private readonly IPlatformRepository _platforms;
    private readonly IStudentRepository _students;
    private readonly ICourseRepository _courses;
    private readonly IStudentWalletRepository _wallets;
    private readonly IFinancialTransactionRepository _transactions;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public PurchaseCourseService(
        IPlatformRepository platforms,
        IStudentRepository students,
        ICourseRepository courses,
        IStudentWalletRepository wallets,
        IFinancialTransactionRepository transactions,
        IEnrollmentRepository enrollments,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _students = students;
        _courses = courses;
        _wallets = wallets;
        _transactions = transactions;
        _enrollments = enrollments;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> PurchaseAsync(
        Guid studentId,
        string? teacherPublicId,
        Guid courseId,
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

            var wallet = await GetOrCreateWalletAsync(studentId, platform.Id, cancellationToken);

            var price = course.Price!.Value;

            if (wallet.Balance < price)
            {
                throw new BusinessRuleViolationException("Insufficient wallet balance.");
            }

            var existing = await _enrollments.GetAsync(studentId, courseId, cancellationToken);

            if (existing is not null && existing.Status == EnrollmentStatus.Active)
            {
                throw new BusinessRuleViolationException("The student already holds an active enrollment in this course.");
            }

            try
            {
                wallet.Debit(price);
            }
            catch (DomainException exception)
            {
                throw new BusinessRuleViolationException(exception.Message, exception);
            }

            var enrollment = new Enrollment(studentId, courseId, platform.Id);

            var transaction = new FinancialTransaction(
                platform.Id,
                wallet.Id,
                studentId,
                TransactionType.Purchase,
                -price,
                wallet.Balance + price,
                wallet.Balance,
                courseId.ToString(),
                studentId,
                "student");

            _enrollments.Add(enrollment);
            _transactions.Add(transaction);
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
