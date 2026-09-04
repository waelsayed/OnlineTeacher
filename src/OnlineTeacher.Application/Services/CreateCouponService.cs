using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Creates a single-use Student Coupon assigned to a specific student and tied to a specific Course
/// within the resolved Teacher Platform. The acting teacher must be a member of the tenant; the
/// <c>Coupon.Manage</c> permission is enforced by the API's permission policy. The assigned student
/// must exist (central identity) and the target course must belong to the same tenant and be a Paid
/// course (Free courses remain ineligible for coupons). Domain invariants validate the code, discount
/// type/value, expiry, and course/student identity.
/// </summary>
public sealed class CreateCouponService
{
    private readonly IPlatformRepository _platforms;
    private readonly ITeacherPlatformAccessRepository _access;
    private readonly IStudentRepository _students;
    private readonly ICourseRepository _courses;
    private readonly IStudentCouponRepository _coupons;
    private readonly IUnitOfWork _unitOfWork;

    public CreateCouponService(
        IPlatformRepository platforms,
        ITeacherPlatformAccessRepository access,
        IStudentRepository students,
        ICourseRepository courses,
        IStudentCouponRepository coupons,
        IUnitOfWork unitOfWork)
    {
        _platforms = platforms;
        _access = access;
        _students = students;
        _courses = courses;
        _coupons = coupons;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> CreateAsync(
        Guid actorTeacherId,
        string? publicId,
        string? code,
        DiscountType discountType,
        decimal discountValue,
        DateTime expiresAt,
        Guid courseId,
        Guid studentId,
        CancellationToken cancellationToken = default)
    {
        var platform = await PlatformResolver.ResolveAsync(_platforms, publicId, cancellationToken);
        await PlatformAccessGuard.RequireMemberAsync(_access, actorTeacherId, platform.Id, cancellationToken);

        if (await _students.GetByIdAsync(studentId, cancellationToken) is null)
        {
            throw new NotFoundException("Assigned student does not exist.");
        }

        var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
            ?? throw new NotFoundException("Course does not exist.");

        if (!course.IsPaid)
        {
            throw new BusinessRuleViolationException("A coupon cannot be assigned to a free course.");
        }

        StudentCoupon coupon;
        try
        {
            coupon = new StudentCoupon(
                platform.Id,
                code ?? string.Empty,
                discountType,
                discountValue,
                expiresAt.ToUniversalTime(),
                courseId,
                studentId,
                actorTeacherId);
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }

        _coupons.Add(coupon);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return coupon.Id;
    }
}
