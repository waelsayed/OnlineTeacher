using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Authenticated Teacher Platform management. The route-template parameters {publicId} and
/// {slug} are validated by TenantRouteMiddleware, which resolves and scopes the tenant.
/// Profile reads and membership listing require <c>Platform.Manage</c>; membership mutations
/// require <c>Platform.Membership</c>. Application services further enforce that the acting
/// user is a member (and owner for mutations) of the resolved tenant, so a valid cross-tenant
/// JWT cannot manage another teacher's platform.
/// </summary>
[ApiController]
[Route("{publicId}/{slug}/api/platform")]
[Authorize]
public sealed class TenantPlatformManagementController : ControllerBase
{
    private readonly GetPlatformProfileService _getProfile;
    private readonly UpdatePlatformProfileService _updateProfile;
    private readonly ListPlatformMembersService _listMembers;
    private readonly AddPlatformMemberService _addMember;
    private readonly ChangePlatformMemberRoleService _changeMemberRole;
    private readonly RemovePlatformMemberService _removeMember;

    public TenantPlatformManagementController(
        GetPlatformProfileService getProfile,
        UpdatePlatformProfileService updateProfile,
        ListPlatformMembersService listMembers,
        AddPlatformMemberService addMember,
        ChangePlatformMemberRoleService changeMemberRole,
        RemovePlatformMemberService removeMember)
    {
        _getProfile = getProfile;
        _updateProfile = updateProfile;
        _listMembers = listMembers;
        _addMember = addMember;
        _changeMemberRole = changeMemberRole;
        _removeMember = removeMember;
    }

    [HttpGet("profile")]
    [RequirePermission("Platform.Manage")]
    public async Task<IActionResult> GetProfile(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var result = await _getProfile.GetAsync(GetTeacherIdClaim(), publicId, cancellationToken);
        return Ok(PlatformProfileResponse.From(result));
    }

    [HttpPut("profile")]
    [RequirePermission("Platform.Manage")]
    public async Task<IActionResult> UpdateProfile(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromBody] UpdatePlatformProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateProfile.UpdateAsync(
            GetTeacherIdClaim(), publicId, request.Name, request.Slug, cancellationToken);
        return Ok(PlatformProfileResponse.From(result));
    }

    [HttpPatch("profile")]
    [RequirePermission("Platform.Manage")]
    public async Task<IActionResult> UpdateProfilePatch(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromBody] UpdatePlatformProfileRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _updateProfile.UpdateAsync(
            GetTeacherIdClaim(), publicId, request.Name, request.Slug, cancellationToken);
        return Ok(PlatformProfileResponse.From(result));
    }

    [HttpGet("members")]
    [RequirePermission("Platform.Manage")]
    public async Task<IActionResult> GetMembers(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var members = await _listMembers.ListAsync(GetTeacherIdClaim(), publicId, cancellationToken);
        return Ok(members.Select(PlatformMemberResponse.From));
    }

    [HttpPost("members")]
    [RequirePermission("Platform.Membership")]
    public async Task<IActionResult> AddMember(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromBody] AddPlatformMemberRequest request,
        CancellationToken cancellationToken)
    {
        var member = await _addMember.AddAsync(
            GetTeacherIdClaim(), publicId, request.Email, cancellationToken);
        return Ok(PlatformMemberResponse.From(member));
    }

    [HttpPut("members/{teacherId}")]
    [RequirePermission("Platform.Membership")]
    public async Task<IActionResult> ChangeMemberRole(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid teacherId,
        [FromBody] ChangePlatformMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var member = await _changeMemberRole.ChangeAsync(
            GetTeacherIdClaim(), publicId, teacherId, request.RoleName, cancellationToken);
        return Ok(PlatformMemberResponse.From(member));
    }

    [HttpPatch("members/{teacherId}")]
    [RequirePermission("Platform.Membership")]
    public async Task<IActionResult> ChangeMemberRolePatch(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid teacherId,
        [FromBody] ChangePlatformMemberRoleRequest request,
        CancellationToken cancellationToken)
    {
        var member = await _changeMemberRole.ChangeAsync(
            GetTeacherIdClaim(), publicId, teacherId, request.RoleName, cancellationToken);
        return Ok(PlatformMemberResponse.From(member));
    }

    [HttpDelete("members/{teacherId}")]
    [RequirePermission("Platform.Membership")]
    public async Task<IActionResult> RemoveMember(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid teacherId,
        CancellationToken cancellationToken)
    {
        await _removeMember.RemoveAsync(GetTeacherIdClaim(), publicId, teacherId, cancellationToken);
        return NoContent();
    }

    private Guid GetTeacherIdClaim()
    {
        var sub = User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);
        var raw = sub?.Value;
        if (raw is null || !Guid.TryParse(raw, out var teacherId))
        {
            throw new UnauthorizedAccessException("The token does not carry a valid teacher identity.");
        }

        return teacherId;
    }
}