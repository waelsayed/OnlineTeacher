using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakePermissionRepository : IPermissionRepository
{
    private readonly Dictionary<string, Permission> _permissionsByCode = new();

    public void Seed(params string[] codes)
    {
        foreach (var code in codes)
        {
            _permissionsByCode[code] = new Permission(code);
        }
    }

    public Task<Permission?> GetByCodeAsync(string code, CancellationToken cancellationToken = default) =>
        Task.FromResult(_permissionsByCode.TryGetValue(code, out var permission) ? permission : null);
}