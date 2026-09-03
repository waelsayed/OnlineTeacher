using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class ListStudentWalletServiceTests
{
    private readonly FakePlatformRepository _platforms = new();
    private readonly FakeStudentRepository _students = new();
    private readonly FakeStudentWalletRepository _wallets = new();
    private readonly FakeFinancialTransactionRepository _transactions = new();
    private readonly StubTenantContext _tenantContext = new();

    private ListStudentWalletService CreateService() => new(_platforms, _students, _wallets, _transactions, _tenantContext);

    private (Student Student, TeacherPlatform Platform) SeedTarget(string publicId = "AbCdEf123456")
    {
        var student = new Student("Sara", Email.Create("sara@example.com"));
        var platform = new TeacherPlatform("My Platform", PublicId.Create(publicId), Slug.CreateFromName("my-platform"));
        _students.Seed(student);
        _platforms.Seed(platform);
        return (student, platform);
    }

    [Fact]
    public async Task Get_NoWallet_ReturnsNull()
    {
        var (student, platform) = SeedTarget();

        var result = await CreateService().GetAsync(student.Id, platform.PublicId.Value);

        result.Should().BeNull();
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Get_ExistingWallet_ReturnsBalanceAndTransactions()
    {
        var (student, platform) = SeedTarget();
        var wallet = new StudentWallet(student.Id, platform.Id);
        wallet.Credit(500m);
        wallet.Debit(150m);
        _wallets.Seed(wallet);
        _transactions.Seed(new FinancialTransaction(
            platform.Id, wallet.Id, student.Id, TransactionType.Credit, 500m, 0m, 500m, null, student.Id, "student"));
        _transactions.Seed(new FinancialTransaction(
            platform.Id, wallet.Id, student.Id, TransactionType.Purchase, -150m, 500m, 350m, "course-id", student.Id, "student"));

        var result = await CreateService().GetAsync(student.Id, platform.PublicId.Value);

        result.Should().NotBeNull();
        result!.WalletId.Should().Be(wallet.Id);
        result.Balance.Should().Be(350m);
        result.Currency.Should().Be("EGP");
        result.Transactions.Should().HaveCount(2);
        _tenantContext.TenantId.Should().BeNull();
    }

    [Fact]
    public async Task Get_UnknownStudent_ThrowsNotFound()
    {
        var (_, platform) = SeedTarget();

        var act = () => CreateService().GetAsync(Guid.NewGuid(), platform.PublicId.Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_InvalidPublicId_ThrowsValidation()
    {
        var (student, _) = SeedTarget();

        var act = () => CreateService().GetAsync(student.Id, "not-a-public-id");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Get_UnknownPlatform_ThrowsNotFound()
    {
        var (student, _) = SeedTarget();

        var act = () => CreateService().GetAsync(student.Id, PublicId.Generate().Value);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Get_UnderTenantContext_ThrowsTenantMismatch()
    {
        var (student, platform) = SeedTarget();
        _tenantContext.TrySetTenant(platform.Id);

        var act = () => CreateService().GetAsync(student.Id, platform.PublicId.Value);

        await act.Should().ThrowAsync<TenantMismatchException>();
    }
}
