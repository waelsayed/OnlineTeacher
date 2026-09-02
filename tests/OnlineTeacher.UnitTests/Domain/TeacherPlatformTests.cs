using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Domain;

public class TeacherPlatformTests
{
    private static TeacherPlatform NewPlatform() =>
        new("My Platform", PublicId.Generate(), Slug.CreateFromName("My Platform"));

    [Fact]
    public void Create_SetsPendingActivationStatus()
    {
        var platform = NewPlatform();

        platform.Status.Should().Be(PlatformStatus.PendingActivation);
        platform.ActivatedAtUtc.Should().BeNull();
    }

    [Fact]
    public void Create_RequiresName()
    {
        var act = () => new TeacherPlatform("  ", PublicId.Generate(), Slug.Create("my-platform"));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresPublicId()
    {
        var act = () => new TeacherPlatform("My Platform", null!, Slug.Create("my-platform"));

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Create_RequiresSlug()
    {
        var act = () => new TeacherPlatform("My Platform", PublicId.Generate(), null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Activate_TransitionsToActiveAndRecordsTimestamp()
    {
        var platform = NewPlatform();

        platform.Activate();

        platform.Status.Should().Be(PlatformStatus.Active);
        platform.ActivatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Activate_WhenAlreadyActive_Throws()
    {
        var platform = NewPlatform();
        platform.Activate();

        var act = platform.Activate;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Activate_WhenDeactivated_Throws()
    {
        var platform = NewPlatform();
        platform.Activate();
        platform.Deactivate();

        var act = platform.Activate;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Deactivate_ActivePlatformTransitionsToDeactivated()
    {
        var platform = NewPlatform();
        platform.Activate();

        platform.Deactivate();

        platform.Status.Should().Be(PlatformStatus.Deactivated);
    }

    [Fact]
    public void Deactivate_WhenPendingActivation_Throws()
    {
        var platform = NewPlatform();

        var act = platform.Deactivate;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ChangeSlug_UpdatesCanonicalSlug()
    {
        var platform = NewPlatform();

        platform.ChangeSlug(Slug.Create("brand-new-canonical-slug"));

        platform.Slug.Value.Should().Be("brand-new-canonical-slug");
    }

    [Fact]
    public void Rename_UpdatesNameAndPreservesIdentity()
    {
        var platform = NewPlatform();
        var id = platform.Id;
        var publicId = platform.PublicId;

        platform.Rename("Updated Name");

        platform.Name.Should().Be("Updated Name");
        platform.Id.Should().Be(id);
        platform.PublicId.Should().Be(publicId);
        platform.UpdatedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void Rename_WhenBlank_Throws()
    {
        var platform = NewPlatform();

        var act = () => platform.Rename("   ");

        act.Should().Throw<DomainException>();
    }
}