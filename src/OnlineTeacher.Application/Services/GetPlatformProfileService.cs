using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Reads the management profile of the resolved Teacher Platform for an authorized actor.
/// The actor must be a member of the tenant; the tenant context is established by the
/// tenant-route middleware, so this simply confirms membership within that tenant.
/// </summary>
public sealed class GetPlatformProfileService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;

    public GetPlatformProfileService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access)
    {
        _platforms = platforms;
        _access = access;
    }

    public async Task<PlatformProfileResult> GetAsync(
        Guid actorTeacherId,
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        return new PlatformProfileResult(
            platform.Id,
            platform.PublicId.Value,
            platform.Name,
            platform.Slug.Value,
            platform.Status);
    }
}