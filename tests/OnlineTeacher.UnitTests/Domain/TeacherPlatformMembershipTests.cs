using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class TeacherPlatformMembershipTests
{
    private static TeacherPlatformMembership NewMembership() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isOwner: true);

    [Fact]
    public void ChangeRole_UpdatesRoleAndOwnerFlag()
    {
        var membership = NewMembership();
        var newRoleId = Guid.NewGuid();

        membership.ChangeRole(newRoleId, isOwner: false);

        membership.RoleId.Should().Be(newRoleId);
        membership.IsOwner.Should().BeFalse();
        membership.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ChangeRole_ToOwner_SetsOwnerFlag()
    {
        var membership = NewMembership();
        var newRoleId = Guid.NewGuid();

        membership.ChangeRole(newRoleId, isOwner: true);

        membership.IsOwner.Should().BeTrue();
        membership.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void ChangeRole_EmptyRoleId_Throws()
    {
        var membership = NewMembership();

        var act = () => membership.ChangeRole(Guid.Empty, isOwner: false);

        act.Should().Throw<DomainException>();
    }
}