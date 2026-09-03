using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ListTransferRequestsServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeTeacherPlatformAccessRepository _access = new();
    private readonly FakeTransferRequestRepository _transferRequests = new();

    private ListTransferRequestsService CreateService() => new(_platforms, _access, _transferRequests);

    private (TeacherPlatform Platform, Guid TeacherId) SeedMember(string publicId = "AbCdEf123456")
    {
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
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
            ["Wallet.Manage"]));
        return (platform, teacherId);
    }

    [Fact]
    public async Task List_ReturnsTenantTransferRequests()
    {
        var (platform, teacherId) = SeedMember();
        var wallet = new StudentWallet(Guid.NewGuid(), platform.Id);
        _transferRequests.Seed(new TransferRequest(wallet.Id, Guid.NewGuid(), platform.Id, 100m, PaymentMethod.InstaPay, "R1"));
        _transferRequests.Seed(new TransferRequest(wallet.Id, Guid.NewGuid(), platform.Id, 200m, PaymentMethod.VodafoneCash, "R2"));

        var result = await CreateService().ListAsync(teacherId, platform.PublicId.Value);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task List_NonMemberActor_ThrowsTenantMismatch()
    {
        var (platform, _) = SeedMember();

        var act = () => CreateService().ListAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task List_InvalidPublicId_ThrowsValidation()
    {
        var (_, teacherId) = SeedMember();

        var act = () => CreateService().ListAsync(teacherId, "not-a-public-id");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task List_UnknownPlatform_ThrowsNotFound()
    {
        var (_, teacherId) = SeedMember();

        var act = () => CreateService().ListAsync(teacherId, PublicId.Generate().Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
