namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Commits the changes staged by the repositories as one atomic unit.
/// Persistence-specific conflicts are translated here into application exceptions.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}