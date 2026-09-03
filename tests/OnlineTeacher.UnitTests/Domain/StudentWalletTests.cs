using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class StudentWalletTests
{
    private static StudentWallet NewWallet(Guid? studentId = null, Guid? tenantId = null) =>
        new(studentId ?? Guid.NewGuid(), tenantId ?? Guid.NewGuid());

    [Fact]
    public void Create_SetsIdentityAndZeroBalance()
    {
        var studentId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var wallet = NewWallet(studentId, tenantId);

        wallet.StudentId.Should().Be(studentId);
        wallet.TenantId.Should().Be(tenantId);
        wallet.Balance.Should().Be(0m);
    }

    [Fact]
    public void Create_RejectsEmptyStudentId()
    {
        var act = () => new StudentWallet(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyTenantId()
    {
        var act = () => new StudentWallet(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Credit_IncreasesBalance()
    {
        var wallet = NewWallet();

        wallet.Credit(500m);

        wallet.Balance.Should().Be(500m);
        wallet.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Credit_NonPositiveAmount_Throws()
    {
        var wallet = NewWallet();

        var act = () => wallet.Credit(0m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Debit_ReducesBalance_WhenSufficient()
    {
        var wallet = NewWallet();
        wallet.Credit(500m);

        wallet.Debit(350m);

        wallet.Balance.Should().Be(150m);
    }

    [Fact]
    public void Debit_NonPositiveAmount_Throws()
    {
        var wallet = NewWallet();
        wallet.Credit(100m);

        var act = () => wallet.Debit(0m);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Debit_WhenInsufficientBalance_Throws()
    {
        var wallet = NewWallet();
        wallet.Credit(100m);

        var act = () => wallet.Debit(150m);

        act.Should().Throw<DomainException>();
        wallet.Balance.Should().Be(100m);
    }

    [Fact]
    public void Debit_NeverMakesBalanceNegative()
    {
        var wallet = NewWallet();
        wallet.Credit(100m);

        var act = () => wallet.Debit(100.01m);

        act.Should().Throw<DomainException>();
        wallet.Balance.Should().Be(100m);
    }
}
