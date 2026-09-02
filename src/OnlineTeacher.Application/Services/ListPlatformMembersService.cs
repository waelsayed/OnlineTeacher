using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Lists the members of the resolved Teacher Platform for an authorized owner. The actor
/// must be a member of the tenant; the management read is further restricted to owners via
/// the permission policy on the endpoint.
/// </summary>
public sealed class ListPlatformMembersService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IPlatformMembershipRepository _memberships;

    public ListPlatformMembersService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IPlatformMembershipRepository memberships)
    {
        _platforms = platforms;
        _access = access;
        _memberships = memberships;
    }

    public async Task<IReadOnlyList<PlatformMember>> ListAsync(
        Guid actorTeacherId,
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        return await _memberships.GetMembersAsync(platform.Id, cancellationToken);
    }
}