using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Domain;

public class TeacherTests
{
    private static Teacher NewTeacher() => new("Wael Sayed", Email.Create("wael@example.com"));

    [Fact]
    public void Create_SetsNameAndEmail()
    {
        var teacher = NewTeacher();

        teacher.Name.Should().Be("Wael Sayed");
        teacher.Email.Value.Should().Be("wael@example.com");
    }

    [Fact]
    public void Create_RejectsEmptyName()
    {
        var act = () => new Teacher("  ", Email.Create("wael@example.com"));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void SetPasswordHash_StoresHash()
    {
        var teacher = NewTeacher();

        teacher.SetPasswordHash("hashed-value");

        teacher.PasswordHash.Should().Be("hashed-value");
    }

    [Fact]
    public void SetPasswordHash_RejectsEmptyHash()
    {
        var teacher = NewTeacher();

        var act = () => teacher.SetPasswordHash(" ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddMembership_AddsMembership()
    {
        var teacher = NewTeacher();
        var platformId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        var membership = new TeacherPlatformMembership(teacher.Id, platformId, roleId, isOwner: true);

        teacher.AddMembership(membership);

        teacher.Memberships.Should().ContainSingle();
    }

    [Fact]
    public void AddMembership_RejectsMembershipForAnotherTeacher()
    {
        var teacher = NewTeacher();
        var membership = new TeacherPlatformMembership(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = () => teacher.AddMembership(membership);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddMembership_RejectsSecondMembershipInSamePlatform()
    {
        var teacher = NewTeacher();
        var platformId = Guid.NewGuid();
        var roleId = Guid.NewGuid();
        teacher.AddMembership(new TeacherPlatformMembership(teacher.Id, platformId, roleId));

        var act = () => teacher.AddMembership(new TeacherPlatformMembership(teacher.Id, platformId, Guid.NewGuid()));

        act.Should().Throw<DomainException>();
    }
}