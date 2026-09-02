using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class FollowTeacherServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakePlatformMembershipRepository _memberships = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeStudentFollowRepository _follows = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private FollowTeacherService CreateService() => new(_platforms, _memberships, _students, _follows, _unitOfWork, _tenantContext);

    private (Student Student, TeacherPlatform Platform, Teacher owner) SeedFollowTarget(
        Guid? studentId = null,
        string publicId = "AbCdEf123456")
    {
        var studentIdValue = studentId ?? Guid.NewGuid();
        var student = new Student("Sara", Email.Create("sara@example.com"));
        if (studentId.HasValue)
        {
            SetId(student, studentIdValue);
        }

        var owner = new Teacher("Teacher", Email.Create($"teacher-{Guid.NewGuid():N}@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        var role = new Role(platform.Id, "Owner");
        var membership = new TeacherPlatformMembership(owner.Id, platform.Id, role.Id, isOwner: true);

        _students.Seed(student);
        _platforms.Seed(platform);
        _memberships.Seed(membership, owner.Name, "Owner");

        return (student, platform, owner);
    }

    private static void SetId(Student student, Guid id)
    {
        typeof(Student).GetProperty(nameof(Student.Id))!.SetValue(student, id);
    }

    [Fact]
    public async Task Follow_ValidTarget_AddsFollowForOwnerTeacher()
    {
        var (student, platform, owner) = SeedFollowTarget();

        await CreateService().FollowAsync(student.Id, platform.PublicId.Value);

        var follow = _follows.Follows.Should().ContainSingle().Subject;
        follow.StudentId.Should().Be(student.Id);
        follow.TeacherId.Should().Be(owner.Id);
        _unitOfWork.SaveCount.Should().Be(1);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Follow_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform, _) = SeedFollowTarget();

        var act = () => CreateService().FollowAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Follow_InvalidPublicId_ThrowsValidation()
    {
        var (student, _, _) = SeedFollowTarget();

        var act = () => CreateService().FollowAsync(student.Id, "not-a-public-id");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Follow_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _, _) = SeedFollowTarget();

        var act = () => CreateService().FollowAsync(student.Id, PublicId.Generate().Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Follow_Duplicate_ThrowsBusinessRuleViolation()
    {
        var (student, platform, owner) = SeedFollowTarget();
        _follows.Seed(new StudentFollow(student.Id, owner.Id));

        var act = () => CreateService().FollowAsync(student.Id, platform.PublicId.Value);

        await act.Should().ThrowAsync<BusinessRuleViolationException>();
    }
}