using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class FinancialTransactionTests
{
    private static FinancialTransaction NewTransaction(
        TransactionType type = TransactionType.Credit,
        decimal amount = 500m,
        decimal before = 0m,
        decimal after = 500m) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            type,
            amount,
            before,
            after,
            "ref-1",
            Guid.NewGuid(),
            "student");

    [Fact]
    public void Create_RecordsLedgerDetails()
    {
        var tenantId = Guid.NewGuid();
        var walletId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var actorId = Guid.NewGuid();

        var tx = new FinancialTransaction(tenantId, walletId, studentId, TransactionType.Purchase, -350m, 500m, 150m, "course-1", actorId, "student");

        tx.TenantId.Should().Be(tenantId);
        tx.WalletId.Should().Be(walletId);
        tx.StudentId.Should().Be(studentId);
        tx.Type.Should().Be(TransactionType.Purchase);
        tx.Status.Should().Be(FinancialTransactionStatus.Completed);
        tx.Amount.Should().Be(-350m);
        tx.BalanceBefore.Should().Be(500m);
        tx.BalanceAfter.Should().Be(150m);
        tx.Reference.Should().Be("course-1");
        tx.ActorId.Should().Be(actorId);
        tx.ActorType.Should().Be("student");
        tx.OccurredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_RejectsEmptyTenantId()
    {
        var act = () => new FinancialTransaction(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), TransactionType.Credit, 100m, 0m, 100m, null, Guid.NewGuid(), "student");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyWalletId()
    {
        var act = () => new FinancialTransaction(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), TransactionType.Credit, 100m, 0m, 100m, null, Guid.NewGuid(), "student");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyStudentId()
    {
        var act = () => new FinancialTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, TransactionType.Credit, 100m, 0m, 100m, null, Guid.NewGuid(), "student");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsBlankActorType()
    {
        var act = () => new FinancialTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Credit, 100m, 0m, 100m, null, Guid.NewGuid(), "   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_TrimsReferenceAndBlankBecomesNull()
    {
        var tx = NewTransaction();
        var blank = new FinancialTransaction(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TransactionType.Credit, 100m, 0m, 100m, "   ", Guid.NewGuid(), "student");

        tx.Reference.Should().NotBeNull();
        blank.Reference.Should().BeNull();
    }
}
