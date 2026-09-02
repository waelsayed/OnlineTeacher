using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class UpdatePlatformProfileServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakeUnitOfWork _unitOfWork = new();

    private UpdatePlatformProfileService CreateService() => new(_platforms, _access, _unitOfWork);

    private static TeacherPlatform SeedPlatform(FakePlatformRepository platforms, string publicId = "AbCdEf123456")
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        platforms.Seed(platform);
        return platform;
    }

    private static TeacherPlatformAccess OwnerAccess(TeacherPlatform platform, Guid teacherId) =>
        new(teacherId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, true, ["Owner"], ["Platform.Access", "Platform.Manage", "Platform.Membership"]);

    private static TeacherPlatformAccess NonOwnerAccess(TeacherPlatform platform, Guid teacherId) =>
        new(teacherId, platform.Id, platform.PublicId.Value, platform.Slug.Value, PlatformStatus.Active, false, ["Assistant"], ["Platform.Access"]);

    [Fact]
    public async Task Update_NameAndSlug_OwnerUpdatesAndSaves()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, OwnerAccess(platform, teacherId));

        var result = await CreateService().UpdateAsync(teacherId, platform.PublicId.Value, "Updated Name", "updated-slug");

        result.Name.Should().Be("Updated Name");
        result.Slug.Should().Be("updated-slug");
        platform.Name.Should().Be("Updated Name");
        platform.Slug.Value.Should().Be("updated-slug");
        platform.PublicId.Value.Should().Be(platform.PublicId.Value);
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Update_NameOnly_KeepsSlug()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, OwnerAccess(platform, teacherId));

        var result = await CreateService().UpdateAsync(teacherId, platform.PublicId.Value, "Only Name", null);

        result.Name.Should().Be("Only Name");
        result.Slug.Should().Be("my-platform");
    }

    [Fact]
    public async Task Update_SlugOnly_KeepsName()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, OwnerAccess(platform, teacherId));

        var result = await CreateService().UpdateAsync(teacherId, platform.PublicId.Value, null, "new-slug");

        result.Name.Should().Be("My Platform");
        result.Slug.Should().Be("new-slug");
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("   ", "  ")]
    public async Task Update_NoChanges_ThrowsValidationException(string? name, string? slug)
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, OwnerAccess(platform, teacherId));

        var act = () => CreateService().UpdateAsync(teacherId, platform.PublicId.Value, name, slug);

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_EmptyName_ThrowsValidationException()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, OwnerAccess(platform, teacherId));

        var act = () => CreateService().UpdateAsync(teacherId, platform.PublicId.Value, "  ", "new-slug");

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_InvalidSlug_ThrowsValidationException()
    {
        var platform = SeedPlatform(_platforms);
        var teacherId = Guid.NewGuid();
        _access.Seed(teacherId, platform.Id, OwnerAccess(platform, teacherId));

        var act = () => CreateService().UpdateAsync(teacherId, platform.PublicId.Value, "Name", "INVALID SLUG");

        await act.Should().ThrowAsync<ValidationException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_NonOwnerMember_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);
        var assistantId = Guid.NewGuid();
        _access.Seed(assistantId, platform.Id, NonOwnerAccess(platform, assistantId));

        var act = () => CreateService().UpdateAsync(assistantId, platform.PublicId.Value, "New Name", null);

        await act.Should().ThrowAsync<TenantMismatchException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task Update_NonMember_ThrowsTenantMismatch()
    {
        var platform = SeedPlatform(_platforms);

        var act = () => CreateService().UpdateAsync(Guid.NewGuid(), platform.PublicId.Value, "New Name", null);

        await act.Should().ThrowAsync<TenantMismatchException>();
        _unitOfWork.SaveCount.Should().Be(0);
    }
}