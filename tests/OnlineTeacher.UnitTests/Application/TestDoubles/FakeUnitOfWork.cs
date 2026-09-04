using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly Action? _onSave;
    private readonly Func<Task>? _onSaveAsync;

    public FakeUnitOfWork(Action? onSave = null, Func<Task>? onSaveAsync = null)
    {
        _onSave = onSave;
        _onSaveAsync = onSaveAsync;
    }

    public int SaveCount { get; private set; }

    public int TransactionCount { get; private set; }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (_onSave is not null)
        {
            _onSave();
        }

        if (_onSaveAsync is not null)
        {
            await _onSaveAsync();
        }

        SaveCount++;
        return SaveCount;
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> action,
        CancellationToken cancellationToken = default)
    {
        TransactionCount++;
        await action(cancellationToken);
    }
}