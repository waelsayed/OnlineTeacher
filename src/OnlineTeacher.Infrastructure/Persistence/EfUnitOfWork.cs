using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Commits the changes staged by the repositories as one atomic unit (EF wraps a single
/// SaveChanges in a transaction). Exposes an explicit-transaction scope so that a sequence of
/// reads and writes (e.g. a <c>SELECT ... FOR UPDATE</c> followed by SaveChanges) run inside one
/// connection transaction opened through the EF execution strategy (preserving retry behavior).
/// Database unique-violation conflicts are translated here into application exceptions so
/// EF/Npgsql details never leak into the API.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public EfUnitOfWork(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
        {
            return HandleDbUpdateException(exception);
        }
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            await action(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });
    }

    private static int HandleDbUpdateException(DbUpdateException exception)
    {
        if (IsUniqueViolation(exception, out var constraintName))
        {
            throw Translate(constraintName, exception);
        }

        throw new ConcurrencyException("A concurrent change prevented this operation from completing.", exception);
    }

    private static bool IsUniqueViolation(DbUpdateException exception, out string constraintName)
    {
        constraintName = (exception.InnerException as PostgresException)?.ConstraintName ?? string.Empty;
        return exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
    }

    private static Exception Translate(string constraintName, DbUpdateException exception) =>
        constraintName switch
        {
            "ux_teachers_email" => new DuplicateEmailException(exception),
            "ux_students_email" => new DuplicateEmailException(exception),
            "ux_follows_student_teacher" => new BusinessRuleViolationException("The student already follows this teacher.", exception),
            "ux_enrollments_student_course" => new BusinessRuleViolationException("The student is already enrolled in this course.", exception),
            "ux_student_wallets_student_tenant" => new BusinessRuleViolationException("A wallet already exists for this student in this platform.", exception),
            "ux_student_coupons_tenant_code" => new BusinessRuleViolationException("A coupon with this code already exists for this platform.", exception),
            _ => new ConcurrencyException("A concurrent change prevented this operation from completing.", exception)
        };
}