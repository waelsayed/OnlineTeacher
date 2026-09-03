using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Lists the wallet credit Transfer Requests submitted to a Teacher Platform. The acting teacher
/// must be a member of the tenant; the Wallet.Manage permission is enforced by the API's permission
/// policy. Requests are listed newest-first and include the submitting student's identity.
/// </summary>
public sealed class ListTransferRequestsService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly ITransferRequestRepository _transferRequests;

    public ListTransferRequestsService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        ITransferRequestRepository transferRequests)
    {
        _platforms = platforms;
        _access = access;
        _transferRequests = transferRequests;
    }

    public async Task<IReadOnlyList<TransferRequestResponse>> ListAsync(
        Guid actorTeacherId,
        string? publicId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        return await _transferRequests.ListByTenantAsync(platform.Id, cancellationToken);
    }
}
