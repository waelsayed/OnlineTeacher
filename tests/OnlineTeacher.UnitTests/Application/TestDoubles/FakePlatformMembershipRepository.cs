using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;

namespace OnlineTeacher.UnitTests.Application.TestDoubles;

internal sealed class FakePlatformMembershipRepository : IPlatformMembershipRepository
{
    private readonly List<TeacherPlatformMembership> _memberships = [];
    private readonly Dictionary<Guid, string> _teacherNames = new();
    private readonly Dictionary<Guid, string> _roleNames = new();
    private readonly Dictionary<Guid, OwnedPlatform> _ownedPlatforms = new();

    public IReadOnlyList<TeacherPlatformMembership> Memberships => _memberships;

    public void Seed(TeacherPlatformMembership membership, string? teacherName = null, string? roleName = null)
    {
        _memberships.Add(membership);
        if (teacherName is not null)
        {
            _teacherNames[membership.TeacherId] = teacherName;
        }

        if (roleName is not null)
        {
            _roleNames[membership.RoleId] = roleName;
        }
    }

    public void SeedOwnedPlatform(Guid platformId, OwnedPlatform platform)
    {
        _ownedPlatforms[platformId] = platform;
    }

    public Task<IReadOnlyList<PlatformMember>> GetMembersAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var members = _memberships
            .Where(m => m.TeacherPlatformId == tenantId)
            .OrderByDescending(m => m.IsOwner)
            .Select(m => new PlatformMember(
                m.TeacherId,
                _teacherNames.GetValueOrDefault(m.TeacherId, m.TeacherId.ToString()),
                m.RoleId,
                _roleNames.GetValueOrDefault(m.RoleId, m.RoleId.ToString()),
                m.IsOwner))
            .ToList();

        return Task.FromResult<IReadOnlyList<PlatformMember>>(members);
    }

    public Task<TeacherPlatformMembership?> GetForTeacherAsync(Guid tenantId, Guid teacherId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_memberships.FirstOrDefault(
            m => m.TeacherPlatformId == tenantId && m.TeacherId == teacherId));

    public Task<Guid?> GetOwnerTeacherIdAsync(Guid tenantId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_memberships.FirstOrDefault(
            m => m.TeacherPlatformId == tenantId && m.IsOwner)?.TeacherId);

    public Task<IReadOnlyList<OwnedPlatform>> GetOwnedPlatformsAsync(Guid teacherId, CancellationToken cancellationToken = default)
    {
        var owned = _memberships
            .Where(m => m.TeacherId == teacherId && m.IsOwner)
            .Select(m => _ownedPlatforms.GetValueOrDefault(
                m.TeacherPlatformId,
                new OwnedPlatform(m.TeacherPlatformId.ToString(), m.TeacherPlatformId.ToString())))
            .ToList();

        return Task.FromResult<IReadOnlyList<OwnedPlatform>>(owned);
    }

    public void Remove(TeacherPlatformMembership membership)
    {
        _memberships.Remove(membership);
    }
}