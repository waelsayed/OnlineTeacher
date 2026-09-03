using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ListStudentEnrollmentsServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeEnrollmentRepository _enrollments = new();
    private readonly StubTenantContext _tenantContext = new();

    private ListStudentEnrollmentsService CreateService() => new(_platforms, _students, _enrollments, _tenantContext);

    private (Student Student, TeacherPlatform Platform) SeedTarget(string publicId = "AbCdEf123456")
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        _students.Seed(student);
        _platforms.Seed(platform);
        return (student, platform);
    }

    [Fact]
    public async Task List_ReturnsStudentEnrollmentsForPlatform()
    {
        var (student, platform) = SeedTarget();
        var courseA = new Course(platform.Id, "Algebra", null);
        var courseB = new Course(platform.Id, "Geometry", null);
        _enrollments.Seed(new Enrollment(student.Id, courseA.Id, platform.Id));
        _enrollments.Seed(new Enrollment(student.Id, courseB.Id, platform.Id));

        var result = await CreateService().ListAsync(student.Id, platform.PublicId.Value);

        result.Should().HaveCount(2);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task List_NoEnrollments_ReturnsEmpty()
    {
        var (student, platform) = SeedTarget();

        var result = await CreateService().ListAsync(student.Id, platform.PublicId.Value);

        result.Should().BeEmpty();
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task List_ExcludesOtherPlatformEnrollments()
    {
        var (student, platform) = SeedTarget();
        var otherPlatform = new TeacherPlatform("Other", PublicId.Create("ZzYyXxWw10ab"), Slug.CreateFromName("other"));
        var course = new Course(platform.Id, "Algebra", null);
        _enrollments.Seed(new Enrollment(student.Id, course.Id, otherPlatform.Id));

        var result = await CreateService().ListAsync(student.Id, platform.PublicId.Value);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task List_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform) = SeedTarget();

        var act = () => CreateService().ListAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task List_InvalidPublicId_ThrowsValidation()
    {
        var (student, _) = SeedTarget();

        var act = () => CreateService().ListAsync(student.Id, "not-a-public-id");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task List_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _) = SeedTarget();

        var act = () => CreateService().ListAsync(student.Id, PublicId.Generate().Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task List_UnderTenantContext_ThrowsTenantMismatch()
    {
        var (student, platform) = SeedTarget();
        _tenantContext.TrySetTenant(platform.Id);

        var act = () => CreateService().ListAsync(student.Id, platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}
