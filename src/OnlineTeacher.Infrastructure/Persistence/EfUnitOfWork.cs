using Microsoft.EntityFrameworkCore;
using Npgsql;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Infrastructure.Persistence;

/// <summary>
/// Commits the changes staged by the repositories as one atomic unit (EF wraps a single
/// SaveChanges in a transaction). Database unique-violation conflicts are translated here
/// into application exceptions so EF/Npgsql details never leak into the API.
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
            _ => new ConcurrencyException("A concurrent change prevented this operation from completing.", exception)
        };
}