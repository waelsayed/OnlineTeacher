using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class TransferRequestTests
{
    private static TransferRequest NewRequest() =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            500m,
            PaymentMethod.VodafoneCash,
            "ref-1");

    [Fact]
    public void Create_SetsPendingAndMetadata()
    {
        var request = NewRequest();

        request.Status.Should().Be(TransferRequestStatus.Pending);
        request.Amount.Should().Be(500m);
        request.PaymentMethod.Should().Be(PaymentMethod.VodafoneCash);
        request.TransferReference.Should().Be("ref-1");
    }

    [Fact]
    public void Create_RejectsNonPositiveAmount()
    {
        var act = () => new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 0m, PaymentMethod.InstaPay);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyWalletId()
    {
        var act = () => new TransferRequest(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), 100m, PaymentMethod.InstaPay);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyStudentId()
    {
        var act = () => new TransferRequest(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), 100m, PaymentMethod.InstaPay);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyTenantId()
    {
        var act = () => new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, 100m, PaymentMethod.InstaPay);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_TrimsReferenceAndBlankBecomesNull()
    {
        var blank = new TransferRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 100m, PaymentMethod.InstaPay, "   ");

        blank.TransferReference.Should().BeNull();
    }

    [Fact]
    public void Approve_TransitionsPendingToApproved()
    {
        var request = NewRequest();

        request.Approve();

        request.Status.Should().Be(TransferRequestStatus.Approved);
        request.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_Throws()
    {
        var request = NewRequest();
        request.Approve();

        var act = () => request.Approve();

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Approve_AfterReject_Throws()
    {
        var request = NewRequest();
        request.Reject();

        var act = () => request.Approve();

        act.Should().Throw<DomainException>();
        request.Status.Should().Be(TransferRequestStatus.Rejected);
    }

    [Fact]
    public void Reject_TransitionsPendingToRejected()
    {
        var request = NewRequest();

        request.Reject();

        request.Status.Should().Be(TransferRequestStatus.Rejected);
        request.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_Throws()
    {
        var request = NewRequest();
        request.Reject();

        var act = () => request.Reject();

        act.Should().Throw<DomainException>();
    }
}
