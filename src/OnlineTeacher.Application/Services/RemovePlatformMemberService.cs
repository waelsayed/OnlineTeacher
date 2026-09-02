using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Removes a member from the resolved Teacher Platform. The acting user must be the
/// platform's owner. The last remaining owner can never be removed, so the platform always
/// keeps a valid owner.
/// </summary>
public sealed class RemovePlatformMemberService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IPlatformMembershipRepository _memberships;
    private readonly IUnitOfWork _unitOfWork;

    public RemovePlatformMemberService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IPlatformMembershipRepository memberships,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _memberships = memberships;
        _unitOfWork = unitOfWork;
    }

    public async Task RemoveAsync(
        Guid actorTeacherId,
        string? publicId,
        Guid memberTeacherId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireOwnerAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        var membership = await _memberships.GetForTeacherAsync(platform.Id, memberTeacherId, cancellationToken)
            ?? throw new NotFoundException("The member is not part of this platform.");

        if (membership.IsOwner)
        {
            await EnsureAnotherOwnerExistsAsync(platform.Id, memberTeacherId, cancellationToken);
        }

        _memberships.Remove(membership);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureAnotherOwnerExistsAsync(Guid tenantId, Guid ownerTeacherId, CancellationToken cancellationToken)
    {
        var members = await _memberships.GetMembersAsync(tenantId, cancellationToken);
        var hasAnotherOwner = members.Any(m => m.IsOwner && m.TeacherId != ownerTeacherId);

        if (!hasAnotherOwner)
        {
            throw new BusinessRuleViolationException("The platform must retain at least one owner.");
        }
    }
}