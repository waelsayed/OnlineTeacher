using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class GetPlatformProfileServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();

    private GetPlatformProfileService CreateService() => new(_platforms, _access);

    private static TeacherPlatform SeedPlatform(FakePlatformRepository platforms, string publicId = "AbCdEf123456")
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        platforms.Seed(platform);
        return platform;
    }

    private static TeacherPlatformAccess AccessFor(TeacherPlatform platform, Guid teacherId, bool isOwner = true) =>
        new(teacherId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, isOwner, ["Owner"], ["Platform.Access", "Platform.Manage"]);

    [Fact]
    public async Task Get_AuthorizedMember_ReturnsProfile()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, AccessFor(platform, teacherId));

        var result = await CreateService().GetAsync(teacherId, platform.PublicId.Value);

        result.PlatformId.Should().Be(platform.Id);
        result.PublicId.Should().Be(platform.PublicId.Value);
        result.Name.Should().Be("My Platform");
        result.Slug.Should().Be("my-platform");
        result.Status.Should().Be(PlatformStatus.PendingActivation);
    }

    [Fact]
    public async Task Get_InvalidPublicId_ThrowsValidationException()
    {
        var act = () => CreateService().GetAsync(Guid.NewGuid(), "not-a-public-id");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Get_UnknownPlatform_ThrowsNotFound()
    {
        var act = () => CreateService().GetAsync(Guid.NewGuid(), PublicId.Generate().Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_NonMemberActor_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);

        var act = () => CreateService().GetAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}