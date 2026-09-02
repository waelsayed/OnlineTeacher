using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakeTeacherPlatformAccessRepository : ITeacherPlatformAccessRepository
{
    private readonly List<(Guid TeacherId, Guid TenantId, TeacherPlatformAccess Access)> _records = [];

    public void Seed(Guid teacherId, Guid tenantId, TeacherPlatformAccess access)
    {
        _records.Add((teacherId, tenantId, access));
    }

    public Task<TeacherPlatformAccess?> GetAsync(Guid teacherId, Guid tenantId, CancellationToken cancellationToken = default)
    {
        TeacherPlatformAccess? access = _records
            .Where(r => r.TeacherId == teacherId && r.TenantId == tenantId)
            .Select(r => r.Access)
            .FirstOrDefault();

        return Task.FromResult(access);
    }
}