namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Commits the changes staged by the repositories as one atomic unit.
/// Persistence-specific conflicts are translated here into application exceptions.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all changes staged since the last save as a single database transaction.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executes the supplied action inside one explicit database transaction (opened with the
    /// EF execution strategy so retry behavior is preserved) and commits at the end. If the action
    /// throws, the transaction is rolled back and nothing is persisted. Reads inside the action
    /// (e.g. <c>SELECT ... FOR UPDATE</c>) therefore hold their locks until commit.
    /// </summary>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default);
}