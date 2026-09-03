using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class CourseServicesTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakeCourseRepository _courses = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private static TeacherPlatform SeedPlatform(FakePlatformRepository platforms, string publicId = "AbCdEf123456")
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        platforms.Seed(platform);
        return platform;
    }

    private static TeacherPlatformAccess AccessFor(Guid teacherId, TeacherPlatform platform, bool isOwner = true) =>
        new(teacherId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, isOwner, ["Owner"], ["Platform.Access", "Platform.Manage"]);

    private (TeacherPlatform Platform, Guid TeacherId) SeedMember()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, AccessFor(teacherId, platform));
        return (platform, teacherId);
    }

    private Course SeedCourse(TeacherPlatform platform)
    {
        var course = new Course(platform.Id, "Algebra", "An algebra course.");
        _courses.Seed(course);
        return course;
    }

    [Fact]
    public async Task CreateCourse_AuthorizedMember_CreatesDraftCourse()
    {
        var (platform, teacherId) = SeedMember();
        var service = new CreateCourseService(_platforms, _access, _courses, _unitOfWork);

        var result = await service.CreateAsync(teacherId, platform.PublicId.Value, "Geometry", null);

        result.Title.Should().Be("Geometry");
        result.Status.Should().Be(CourseStatus.Draft);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateCourse_NonMemberActor_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();
        var service = new CreateCourseService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.CreateAsync(Guid.NewGuid(), platform.PublicId.Value, "Geometry", null);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task CreateCourse_BlankTitle_ThrowsValidationException()
    {
        var (platform, teacherId) = SeedMember();
        var service = new CreateCourseService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.CreateAsync(teacherId, platform.PublicId.Value, "   ", null);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task CreateCourse_UnknownPlatform_ThrowsNotFound()
    {
        var service = new CreateCourseService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.CreateAsync(Guid.NewGuid(), PublicId.Generate().Value, "Geometry", null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task CreateCourse_InvalidPublicId_ThrowsValidationException()
    {
        var service = new CreateCourseService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.CreateAsync(Guid.NewGuid(), "not-a-public-id", "Geometry", null);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task GetCourse_AuthorizedMember_ReturnsFullDetail()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        saved.AddUnit("Unit One").AddLesson("Lesson One");

        var service = new GetCourseService(_platforms, _access, _courses);

        var result = await service.GetAsync(teacherId, platform.PublicId.Value, saved.Id);

        result.Title.Should().Be("Algebra");
        result.Units.Should().HaveCount(1);
        result.Units[0].Position.Should().Be(1);
        result.Units[0].Lessons.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetCourse_UnknownCourse_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var service = new GetCourseService(_platforms, _access, _courses);

        var act = () => service.GetAsync(teacherId, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCourse_NonMemberActor_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();
        var saved = SeedCourse(platform);
        var service = new GetCourseService(_platforms, _access, _courses);

        var act = () => service.GetAsync(Guid.NewGuid(), platform.PublicId.Value, saved.Id);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task ListCourses_ReturnsOnlyTenantCoursesOrderedByTitle()
    {
        var (platform, teacherId) = SeedMember();
        _courses.Seed(new Course(platform.Id, "Chemistry", null));
        _courses.Seed(new Course(platform.Id, "Algebra", null));

        var service = new ListCoursesService(_platforms, _access, _courses);

        var result = await service.ListAsync(teacherId, platform.PublicId.Value);

        result.Select(c => c.Title).Should().Equal("Algebra", "Chemistry");
    }

    [Fact]
    public async Task UpdateCourse_PublishesStatus()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var service = new UpdateCourseService(_platforms, _access, _courses, _unitOfWork);

        var result = await service.UpdateAsync(teacherId, platform.PublicId.Value, saved.Id, null, null, CourseStatus.Published);

        result.Status.Should().Be(CourseStatus.Published);
    }

    [Fact]
    public async Task UpdateCourse_UnknownCourse_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var service = new UpdateCourseService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.UpdateAsync(teacherId, platform.PublicId.Value, Guid.NewGuid(), null, null, null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteCourse_RemovesCourseAndSaves()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var service = new DeleteCourseService(_platforms, _access, _courses, _unitOfWork);

        await service.DeleteAsync(teacherId, platform.PublicId.Value, saved.Id);

        _courses.Courses.Should().NotContain(saved);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task DeleteCourse_UnknownCourse_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var service = new DeleteCourseService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.DeleteAsync(teacherId, platform.PublicId.Value, Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task AddUnit_AppendsUnit()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var service = new AddUnitService(_platforms, _access, _courses, _unitOfWork);

        var result = await service.AddAsync(teacherId, platform.PublicId.Value, saved.Id, "Unit One", null);

        result.Title.Should().Be("Unit One");
        result.Position.Should().Be(1);
        saved.Units.Should().HaveCount(1);
    }

    [Fact]
    public async Task AddUnit_UnknownCourse_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var service = new AddUnitService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.AddAsync(teacherId, platform.PublicId.Value, Guid.NewGuid(), "Unit One", null);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task UpdateUnit_MovesUnit()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var a = saved.AddUnit("A");
        saved.AddUnit("B");
        var service = new UpdateUnitService(_platforms, _access, _courses, _unitOfWork);

        var result = await service.UpdateAsync(teacherId, platform.PublicId.Value, saved.Id, a.Id, null, 2);

        result.Position.Should().Be(2);
    }

    [Fact]
    public async Task RemoveUnit_RemovesUnit()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var unit = saved.AddUnit("Unit One");
        var service = new RemoveUnitService(_platforms, _access, _courses, _unitOfWork);

        await service.RemoveAsync(teacherId, platform.PublicId.Value, saved.Id, unit.Id);

        saved.Units.Should().BeEmpty();
    }

    [Fact]
    public async Task AddLesson_AppendsLesson()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var unit = saved.AddUnit("Unit One");
        var service = new AddLessonService(_platforms, _access, _courses, _unitOfWork);

        var result = await service.AddAsync(teacherId, platform.PublicId.Value, saved.Id, unit.Id, "Lesson One", null);

        result.Title.Should().Be("Lesson One");
        result.Position.Should().Be(1);
        unit.Lessons.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateLesson_MovesLesson()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var unit = saved.AddUnit("Unit One");
        var l1 = unit.AddLesson("A");
        unit.AddLesson("B");
        var service = new UpdateLessonService(_platforms, _access, _courses, _unitOfWork);

        var result = await service.UpdateAsync(teacherId, platform.PublicId.Value, saved.Id, unit.Id, l1.Id, null, 2);

        result.Position.Should().Be(2);
    }

    [Fact]
    public async Task RemoveLesson_RemovesLesson()
    {
        var (platform, teacherId) = SeedMember();
        var saved = SeedCourse(platform);
        var unit = saved.AddUnit("Unit One");
        var lesson = unit.AddLesson("Lesson One");
        var service = new RemoveLessonService(_platforms, _access, _courses, _unitOfWork);

        await service.RemoveAsync(teacherId, platform.PublicId.Value, saved.Id, unit.Id, lesson.Id);

        unit.Lessons.Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveLesson_OnUnknownCourse_ThrowsNotFound()
    {
        var (platform, teacherId) = SeedMember();
        var service = new RemoveLessonService(_platforms, _access, _courses, _unitOfWork);

        var act = () => service.RemoveAsync(teacherId, platform.PublicId.Value, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}