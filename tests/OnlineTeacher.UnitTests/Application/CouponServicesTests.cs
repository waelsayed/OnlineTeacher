using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class CouponServicesTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeStudentCouponRepository _coupons = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static TeacherPlatform NewPlatform(string publicId = "AbCdEf123456") =>
        new("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));

    private (TeacherPlatform Platform, Guid TeacherId) SeedMember(string publicId = "AbCdEf123456")
    {
        var platform = NewPlatform(publicId);
        _platforms.Seed(platform);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, new TeacherPlatformAccess(
            teacherId,
            platform.Id,
            platform.PublicId.Value,
            platform.Slug.Value,
            PlatformStatus.Active,
            true,
            ["Owner"],
            ["Coupon.Manage"]));
        return (platform, teacherId);
    }

    private Course SeedPaidCourse(TeacherPlatform platform, decimal price = 100m)
    {
        var course = new Course(platform.Id, "Algebra", "An algebra course.", CoursePricingType.Paid, price);
        course.Publish();
        _courses.Seed(course);
        return course;
    }

    private Student SeedStudent(Guid id) =>
        new("Sara", Email.Create("sara" + id.ToString("N")[..6] + "@example.com"));

    private static StudentCoupon NewCoupon(
        Guid tenantId,
        Guid courseId,
        Guid studentId,
        Guid teacherId,
        string code = "SAVE50",
        DiscountType discountType = DiscountType.Percentage,
        decimal discountValue = 50m,
        DateTime? expiresAt = null) =>
        new(
            tenantId,
            code,
            discountType,
            discountValue,
            expiresAt ?? DateTime.UtcNow.AddDays(30),
            courseId,
            studentId,
            teacherId);

    [Fact]
    public async Task Create_ValidCoupon_AddsAndSaves()
    {
        var (platform, teacherId) = SeedMember();
        var course = SeedPaidCourse(platform);
        var student = SeedStudent(Guid.NewGuid());
        _students.Seed(student);

        var id = await new CreateCouponService(
                _platforms, _access, _students, _courses, _coupons, _unitOfWork)
            .CreateAsync(teacherId, platform.PublicId.Value, "save50", DiscountType.Percentage, 50m,
                DateTime.UtcNow.AddDays(30), course.Id, student.Id);

        _coupons.Coupons.Should().ContainSingle().Which.Code.Should().Be("SAVE50");
        _coupons.Coupons[0].TenantId.Should().Be(platform.Id);
        _coupons.Coupons[0].CourseId.Should().Be(course.Id);
        _coupons.Coupons[0].AssignedToStudentId.Should().Be(student.Id);
        _coupons.Coupons[0].Id.Should().Be(id);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Create_NonMember_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();
        var course = SeedPaidCourse(platform);
        var student = SeedStudent(Guid.NewGuid());
        _students.Seed(student);

        var act = () => new CreateCouponService(
                _platforms, _access, _students, _courses, _coupons, _unitOfWork)
            .CreateAsync(Guid.NewGuid(), platform.PublicId.Value, "SAVE50", DiscountType.Percentage, 50m,
                DateTime.UtcNow.AddDays(30), course.Id, student.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task Create_UnknownStudent_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var course = SeedPaidCourse(platform);

        var act = () => new CreateCouponService(
                _platforms, _access, _students, _courses, _coupons, _unitOfWork)
            .CreateAsync(teacherId, platform.PublicId.Value, "SAVE50", DiscountType.Percentage, 50m,
                DateTime.UtcNow.AddDays(30), course.Id, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_UnknownCourse_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var student = SeedStudent(Guid.NewGuid());
        _students.Seed(student);

        var act = () => new CreateCouponService(
                _platforms, _access, _students, _courses, _coupons, _unitOfWork)
            .CreateAsync(teacherId, platform.PublicId.Value, "SAVE50", DiscountType.Percentage, 50m,
                DateTime.UtcNow.AddDays(30), Guid.NewGuid(), student.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Create_FreeCourse_ThrowsBusinessRuleViolation()
    {
        var (platform, teacherId) = SeedMember();
        var course = new Course(platform.Id, "Free", null, CoursePricingType.Free);
        course.Publish();
        _courses.Seed(course);
        var student = SeedStudent(Guid.NewGuid());
        _students.Seed(student);

        var act = () => new CreateCouponService(
                _platforms, _access, _students, _courses, _coupons, _unitOfWork)
            .CreateAsync(teacherId, platform.PublicId.Value, "SAVE50", DiscountType.Percentage, 50m,
                DateTime.UtcNow.AddDays(30), course.Id, student.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>()
            .WithMessage("*free course*");
    }

    [Fact]
    public async Task Create_InvalidDiscount_ThrowsValidation()
    {
        var (platform, teacherId) = SeedMember();
        var course = SeedPaidCourse(platform);
        var student = SeedStudent(Guid.NewGuid());
        _students.Seed(student);

        var act = () => new CreateCouponService(
                _platforms, _access, _students, _courses, _coupons, _unitOfWork)
            .CreateAsync(teacherId, platform.PublicId.Value, "SAVE50", DiscountType.Percentage, 101m,
                DateTime.UtcNow.AddDays(30), course.Id, student.Id);

        await act.Should().ThrowAsync<ValidationException>();
        _coupons.Coupons.Should().BeEmpty();
    }

    [Fact]
    public async Task List_ReturnsTenantCouponsWithStudentName()
    {
        var (platform, teacherId) = SeedMember();
        var student = SeedStudent(Guid.NewGuid());
        _students.Seed(student);
        _coupons.Seed(NewCoupon(platform.Id, Guid.NewGuid(), student.Id, teacherId, code: "AAA1"));
        _coupons.Seed(NewCoupon(platform.Id, Guid.NewGuid(), student.Id, teacherId, code: "BBB2"));
        var otherPlatform = NewPlatform("XyZwAb345678");
        _platforms.Seed(otherPlatform);
        _coupons.Seed(NewCoupon(otherPlatform.Id, Guid.NewGuid(), student.Id, teacherId, code: "OTHER1"));

        var result = await new ListCouponsService(_platforms, _access, _coupons, _students)
            .ListAsync(teacherId, platform.PublicId.Value, null);

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(dto => dto.StudentName.Should().Be("Sara"));
    }

    [Fact]
    public async Task List_FilterByStatus_ReturnsOnlyMatching()
    {
        var (platform, teacherId) = SeedMember();
        _coupons.Seed(NewCoupon(platform.Id, Guid.NewGuid(), Guid.NewGuid(), teacherId, code: "ACTV1"));
        var consumed = NewCoupon(platform.Id, Guid.NewGuid(), Guid.NewGuid(), teacherId, code: "CONS1");
        consumed.Consume(consumed.AssignedToStudentId, consumed.CourseId, Guid.NewGuid());
        _coupons.Seed(consumed);

        var result = await new ListCouponsService(_platforms, _access, _coupons, _students)
            .ListAsync(teacherId, platform.PublicId.Value, CouponStatus.Consumed);

        result.Should().ContainSingle().Which.Code.Should().Be("CONS1");
    }

    [Fact]
    public async Task List_NonMember_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();

        var act = () => new ListCouponsService(_platforms, _access, _coupons, _students)
            .ListAsync(Guid.NewGuid(), platform.PublicId.Value, null);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task Get_ReturnsCouponOfTenant()
    {
        var (platform, teacherId) = SeedMember();
        var student = SeedStudent(Guid.NewGuid());
        _students.Seed(student);
        var coupon = NewCoupon(platform.Id, Guid.NewGuid(), student.Id, teacherId, code: "GETME1");
        _coupons.Seed(coupon);

        var dto = await new GetCouponService(_platforms, _access, _coupons, _students)
            .GetAsync(teacherId, platform.PublicId.Value, coupon.Id);

        dto.Should().NotBeNull();
        dto.Code.Should().Be("GETME1");
        dto.StudentName.Should().Be("Sara");
    }

    [Fact]
    public async Task Get_CouponOfAnotherTenant_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var otherPlatform = NewPlatform("XyZwAb345678");
        _platforms.Seed(otherPlatform);
        var otherTeacher = Guid.NewGuid();
        _access.Seed(otherTeacher, otherPlatform.Id, new TeacherPlatformAccess(
            otherTeacher,
            otherPlatform.Id,
            otherPlatform.PublicId.Value,
            otherPlatform.Slug.Value,
            PlatformStatus.Active,
            true,
            ["Owner"],
            ["Coupon.Manage"]));
        var coupon = NewCoupon(otherPlatform.Id, Guid.NewGuid(), Guid.NewGuid(), otherTeacher, code: "OTHER1");
        _coupons.Seed(coupon);

        var act = () => new GetCouponService(_platforms, _access, _coupons, _students)
            .GetAsync(teacherId, platform.PublicId.Value, coupon.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_UnknownCoupon_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();

        var act = () => new GetCouponService(_platforms, _access, _coupons, _students)
            .GetAsync(teacherId, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Revoke_ActiveCoupon_MarksExpiredAndSaves()
    {
        var (platform, teacherId) = SeedMember();
        var coupon = NewCoupon(platform.Id, Guid.NewGuid(), Guid.NewGuid(), teacherId, code: "REVK1");
        _coupons.Seed(coupon);

        await new RevokeCouponService(_platforms, _access, _coupons, _unitOfWork)
            .RevokeAsync(teacherId, platform.PublicId.Value, coupon.Id);

        coupon.Status.Should().Be(CouponStatus.Expired);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Revoke_ConsumedCoupon_ThrowsBusinessRuleViolation()
    {
        var (platform, teacherId) = SeedMember();
        var coupon = NewCoupon(platform.Id, Guid.NewGuid(), Guid.NewGuid(), teacherId, code: "REVK2");
        coupon.Consume(coupon.AssignedToStudentId, coupon.CourseId, Guid.NewGuid());
        _coupons.Seed(coupon);

        var act = () => new RevokeCouponService(_platforms, _access, _coupons, _unitOfWork)
            .RevokeAsync(teacherId, platform.PublicId.Value, coupon.Id);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }

    [Fact]
    public async Task Revoke_CouponOfAnotherTenant_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var otherPlatform = NewPlatform("XyZwAb345678");
        _platforms.Seed(otherPlatform);
        var otherTeacher = Guid.NewGuid();
        _access.Seed(otherTeacher, otherPlatform.Id, new TeacherPlatformAccess(
            otherTeacher,
            otherPlatform.Id,
            otherPlatform.PublicId.Value,
            otherPlatform.Slug.Value,
            PlatformStatus.Active,
            true,
            ["Owner"],
            ["Coupon.Manage"]));
        var coupon = NewCoupon(otherPlatform.Id, Guid.NewGuid(), Guid.NewGuid(), otherTeacher, code: "OTHER1");
        _coupons.Seed(coupon);

        var act = () => new RevokeCouponService(_platforms, _access, _coupons, _unitOfWork)
            .RevokeAsync(teacherId, platform.PublicId.Value, coupon.Id);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Revoke_NonMemberActor_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();
        var coupon = NewCoupon(platform.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), code: "REVK3");
        _coupons.Seed(coupon);

        var act = () => new RevokeCouponService(_platforms, _access, _coupons, _unitOfWork)
            .RevokeAsync(Guid.NewGuid(), platform.PublicId.Value, coupon.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Theory]
    [InlineData("not-a-public-id")]
    public async Task Get_InvalidPublicId_ThrowsValidation(string badPublicId)
    {
        var (_, teacherId) = SeedMember();

        var act = () => new GetCouponService(_platforms, _access, _coupons, _students)
            .GetAsync(teacherId, badPublicId, Guid.NewGuid());

        await act.Should().ThrowAsync<ValidationException>();
    }
}
