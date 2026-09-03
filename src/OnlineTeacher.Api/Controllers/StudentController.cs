using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineTeacher.Api.Authentication;
using OnlineTeacher.Api.Authorization;
using OnlineTeacher.Api.Contracts;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Api.Controllers;

/// <summary>
/// Central Student identity and following. Registration and login are anonymous; all other
/// endpoints require an authenticated <b>Student</b> principal (a teacher JWT is rejected via
/// the <see cref="RequirePrincipalTypeAttribute"/> policy). These routes are central and carry
/// no tenant route, so they never enter a Teacher Platform tenant scope.
/// </summary>
[ApiController]
[Route("api/student")]
public sealed class StudentController : ControllerBase
{
    private readonly RegisterStudentService _register;
    private readonly AuthenticateStudentService _authenticate;
    private readonly GetStudentProfileService _profile;
    private readonly FollowTeacherService _follow;
    private readonly UnfollowTeacherService _unfollow;
    private readonly ListFollowedTeachersService _listFollowing;
    private readonly IsFollowingTeacherService _isFollowing;
    private readonly EnrollStudentService _enroll;
    private readonly ListStudentEnrollmentsService _listEnrollments;
    private readonly CancelEnrollmentService _cancelEnrollment;
    private readonly SubmitTransferRequestService _submitTransfer;
    private readonly ListStudentWalletService _listWallet;
    private readonly PurchaseCourseService _purchase;
    private readonly JwtTokenFactory _jwt;

    public StudentController(
        RegisterStudentService register,
        AuthenticateStudentService authenticate,
        GetStudentProfileService profile,
        FollowTeacherService follow,
        UnfollowTeacherService unfollow,
        ListFollowedTeachersService listFollowing,
        IsFollowingTeacherService isFollowing,
        EnrollStudentService enroll,
        ListStudentEnrollmentsService listEnrollments,
        CancelEnrollmentService cancelEnrollment,
        SubmitTransferRequestService submitTransfer,
        ListStudentWalletService listWallet,
        PurchaseCourseService purchase,
        JwtTokenFactory jwt)
    {
        _register = register;
        _authenticate = authenticate;
        _profile = profile;
        _follow = follow;
        _unfollow = unfollow;
        _listFollowing = listFollowing;
        _isFollowing = isFollowing;
        _enroll = enroll;
        _listEnrollments = listEnrollments;
        _cancelEnrollment = cancelEnrollment;
        _submitTransfer = submitTransfer;
        _listWallet = listWallet;
        _purchase = purchase;
        _jwt = jwt;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterStudentRequest request, CancellationToken cancellationToken)
    {
        var result = await _register.RegisterAsync(request.Name, request.Email, request.Password, cancellationToken);
        return Created(string.Empty, new RegisterStudentResponse(result.StudentId));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] StudentLoginRequest request, CancellationToken cancellationToken)
    {
        var authentication = await _authenticate.AuthenticateAsync(request.Email, request.Password, cancellationToken);

        if (!authentication.Succeeded || authentication.StudentId is null)
        {
            return Unauthorized(new
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Unauthorized",
                Detail = authentication.FailureMessage
            });
        }

        return Ok(new StudentLoginResponse(
            _jwt.CreateStudent(authentication.StudentId.Value),
            authentication.StudentId.Value));
    }

    [HttpGet("me")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        var profile = await _profile.GetAsync(studentId, cancellationToken);
        return Ok(new StudentProfileResponse(profile.StudentId, profile.Name, profile.Email));
    }

    [HttpPost("follow/{teacherPublicId}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Follow(string teacherPublicId, CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        await _follow.FollowAsync(studentId, teacherPublicId, cancellationToken);
        return Ok();
    }

    [HttpDelete("follow/{teacherPublicId}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Unfollow(string teacherPublicId, CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        await _unfollow.UnfollowAsync(studentId, teacherPublicId, cancellationToken);
        return NoContent();
    }

    [HttpGet("following")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Following(CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        var followed = await _listFollowing.ListAsync(studentId, cancellationToken);
        var response = followed
            .Select(f => new FollowedTeacherResponse(f.PublicId, f.Slug))
            .ToList();
        return Ok(response);
    }

    [HttpGet("following/{teacherPublicId}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> IsFollowing(string teacherPublicId, CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        var result = await _isFollowing.IsFollowingAsync(studentId, teacherPublicId, cancellationToken);
        return Ok(new { follows = result });
    }

    [HttpPost("enroll/{teacherPublicId}/{courseId:guid}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Enroll(
        string teacherPublicId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        var enrollmentId = await _enroll.EnrollAsync(studentId, teacherPublicId, courseId, cancellationToken);
        return Created(string.Empty, new { enrollmentId });
    }

    [HttpGet("enrollments/{teacherPublicId}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Enrollments(string teacherPublicId, CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        var enrollments = await _listEnrollments.ListAsync(studentId, teacherPublicId, cancellationToken);
        return Ok(enrollments.Select(EnrollmentListItemResponse.From));
    }

    [HttpDelete("enrollments/{teacherPublicId}/{courseId:guid}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> CancelEnrollment(
        string teacherPublicId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        await _cancelEnrollment.CancelAsync(studentId, teacherPublicId, courseId, cancellationToken);
        return NoContent();
    }

    [HttpGet("wallet/{teacherPublicId}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Wallet(string teacherPublicId, CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        var wallet = await _listWallet.GetAsync(studentId, teacherPublicId, cancellationToken);
        return wallet is null ? Ok((WalletDetailResponse?)null) : Ok(WalletDetailResponse.From(wallet));
    }

    [HttpPost("wallet/{teacherPublicId}/transfer")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> SubmitTransfer(
        string teacherPublicId,
        [FromBody] SubmitTransferRequest request,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();

        if (!Enum.TryParse<PaymentMethod>(request.PaymentMethod, ignoreCase: true, out var paymentMethod))
        {
            return BadRequest(new
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Bad Request",
                Detail = "The payment method is not supported."
            });
        }

        var transferId = await _submitTransfer.SubmitAsync(
            studentId,
            teacherPublicId,
            request.Amount,
            paymentMethod,
            request.TransferReference,
            cancellationToken);

        return Created(string.Empty, new { transferId });
    }

    [HttpPost("purchase/{teacherPublicId}/{courseId:guid}")]
    [Authorize]
    [RequirePrincipalType(PrincipalTypes.Student)]
    public async Task<IActionResult> Purchase(
        string teacherPublicId,
        Guid courseId,
        CancellationToken cancellationToken)
    {
        var studentId = GetStudentIdClaim();
        var enrollmentId = await _purchase.PurchaseAsync(studentId, teacherPublicId, courseId, cancellationToken);
        return Created(string.Empty, new { enrollmentId });
    }

    private Guid GetStudentIdClaim()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub);
        var raw = sub?.Value;
        if (raw is null || !Guid.TryParse(raw, out var studentId))
        {
            throw new UnauthorizedAccessException("The token does not carry a valid student identity.");
        }

        return studentId;
    }
}