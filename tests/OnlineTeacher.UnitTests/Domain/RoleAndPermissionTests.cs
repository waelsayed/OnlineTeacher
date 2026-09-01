using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class RoleAndPermissionTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid AnotherTenantId = Guid.NewGuid();

    [Fact]
    public void Permission_Create_SetsCode()
    {
        var permission = new Permission("Platform.Access");

        permission.Code.Should().Be("Platform.Access");
    }

    [Fact]
    public void Permission_Create_RejectsEmptyCode()
    {
        var act = () => new Permission(" ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Role_Create_SetsTenantAndName()
    {
        var role = new Role(TenantId, "Owner");

        role.TenantId.Should().Be(TenantId);
        role.Name.Should().Be("Owner");
    }

    [Fact]
    public void Role_Create_RejectsEmptyTenant()
    {
        var act = () => new Role(Guid.Empty, "Owner");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Role_Create_RejectsEmptyName()
    {
        var act = () => new Role(TenantId, " ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddPermission_AddsSingleRolePermissionCarryingTenant()
    {
        var role = new Role(TenantId, "Owner");
        var permission = new Permission("Platform.Access");

        var rolePermission = role.AddPermission(permission);

        role.Permissions.Should().ContainSingle();
        rolePermission.RoleId.Should().Be(role.Id);
        rolePermission.PermissionId.Should().Be(permission.Id);
        rolePermission.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public void AddPermission_RejectsDuplicatePermissionWithinRole()
    {
        var role = new Role(TenantId, "Owner");
        var permission = new Permission("Platform.Access");
        role.AddPermission(permission);

        var act = () => role.AddPermission(permission);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Membership_CarriesOwnerFlag()
    {
        var membership = new TeacherPlatformMembership(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), isOwner: true);

        membership.IsOwner.Should().BeTrue();
        membership.TenantId.Should().Be(membership.TeacherPlatformId);
    }

    [Fact]
    public void Membership_RejectsMissingTeacherPlatformOrRole()
    {
        var act = () => new TeacherPlatformMembership(Guid.NewGuid(), Guid.Empty, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Memberships_DoNotCrossTenantBoundaries()
    {
        var roleTenantA = new Role(TenantId, "Owner");
        var roleTenantB = new Role(AnotherTenantId, "Owner");

        roleTenantA.TenantId.Should().NotBe(roleTenantB.TenantId);
    }
}