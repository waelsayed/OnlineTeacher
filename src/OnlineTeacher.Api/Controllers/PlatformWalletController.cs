using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Teacher Platform wallet credit (Transfer Request) management. The route-template parameters
/// {publicId} and {slug} are validated by TenantRouteMiddleware, which resolves and scopes the
/// tenant. Listing and reviewing transfer requests require the <c>Wallet.Manage</c> permission.
/// The application services further enforce that the acting user is a member of the resolved
/// tenant, so a valid cross-tenant JWT cannot manage another platform's wallet.
/// </summary>
[ApiController]
[Route("{publicId}/{slug}/api/platform/wallet")]
[Authorize]
public sealed class PlatformWalletController : ControllerBase
{
    private readonly ListTransferRequestsService _listTransfers;
    private readonly ReviewTransferRequestService _reviewTransfer;

    public PlatformWalletController(
        ListTransferRequestsService listTransfers,
        ReviewTransferRequestService reviewTransfer)
    {
        _listTransfers = listTransfers;
        _reviewTransfer = reviewTransfer;
    }

    [HttpGet("transfers")]
    [RequirePermission("Wallet.Manage")]
    public async Task<IActionResult> ListTransfers(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        CancellationToken cancellationToken)
    {
        var transfers = await _listTransfers.ListAsync(GetTeacherIdClaim(), publicId, cancellationToken);
        return Ok(transfers.Select(WalletTransferRequestResponse.From));
    }

    [HttpPost("transfers/{transferId:guid}/approve")]
    [RequirePermission("Wallet.Manage")]
    public async Task<IActionResult> ApproveTransfer(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid transferId,
        CancellationToken cancellationToken)
    {
        await _reviewTransfer.ApproveAsync(GetTeacherIdClaim(), publicId, transferId, cancellationToken);
        return Ok();
    }

    [HttpPost("transfers/{transferId:guid}/reject")]
    [RequirePermission("Wallet.Manage")]
    public async Task<IActionResult> RejectTransfer(
        [FromRoute] string publicId,
        [FromRoute] string slug,
        [FromRoute] Guid transferId,
        CancellationToken cancellationToken)
    {
        await _reviewTransfer.RejectAsync(GetTeacherIdClaim(), publicId, transferId, cancellationToken);
        return Ok();
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
