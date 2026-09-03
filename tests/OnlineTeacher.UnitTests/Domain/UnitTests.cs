using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class UnitTests
{
    private static Unit NewUnit(string title = "Unit One") =>
        new(Guid.NewGuid(), Guid.NewGuid(), title, 1);

    [Fact]
    public void Create_SetsPositionAndSnapshotsTenantAndCourse()
    {
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var unit = new Unit(courseId, tenantId, "Unit One", 3);

        unit.CourseId.Should().Be(courseId);
        unit.TenantId.Should().Be(tenantId);
        unit.Position.Should().Be(3);
    }

    [Fact]
    public void Create_RequiresCourse()
    {
        var act = () => new Unit(Guid.Empty, Guid.NewGuid(), "Unit One", 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresTenant()
    {
        var act = () => new Unit(Guid.NewGuid(), Guid.Empty, "Unit One", 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresTitle()
    {
        var act = () => new Unit(Guid.NewGuid(), Guid.NewGuid(), "   ", 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresPositivePosition()
    {
        var act = () => new Unit(Guid.NewGuid(), Guid.NewGuid(), "Unit One", 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rename_TrimsTitle()
    {
        var unit = NewUnit();

        unit.Rename("  Updated  ");

        unit.Title.Should().Be("Updated");
    }

    [Fact]
    public void Rename_WithBlankTitle_Throws()
    {
        var unit = NewUnit();

        var act = () => unit.Rename("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddLesson_AppendsAtNextPosition()
    {
        var unit = NewUnit();

        unit.AddLesson("Lesson One");
        unit.AddLesson("Lesson Two");

        unit.Lessons.Should().HaveCount(2);
        unit.Lessons[0].Position.Should().Be(1);
        unit.Lessons[1].Position.Should().Be(2);
    }

    [Fact]
    public void AddLesson_AtExplicitPosition_ShiftsLaterLessons()
    {
        var unit = NewUnit();
        unit.AddLesson("First");
        unit.AddLesson("Second");
        var third = unit.AddLesson("Third");

        var inserted = unit.AddLesson("Inserted", 2);

        inserted.Position.Should().Be(2);
        third.Position.Should().Be(4);
        unit.Lessons.Select(l => l.Position).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void AddLesson_WithBlankTitle_Throws()
    {
        var unit = NewUnit();

        var act = () => unit.AddLesson("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveLesson_KeepsPositionsContiguous()
    {
        var unit = NewUnit();
        unit.AddLesson("A");
        unit.AddLesson("B");
        unit.AddLesson("C");
        var target = unit.Lessons[0];

        unit.MoveLesson(target, 3);

        unit.Lessons.Select(l => l.Position).Should().Equal(1, 2, 3);
        unit.Lessons.Single(l => l.Id == target.Id).Position.Should().Be(3);
    }

    [Fact]
    public void RemoveLesson_ReindexesRemainingLessons()
    {
        var unit = NewUnit();
        unit.AddLesson("A");
        var middle = unit.AddLesson("B");
        unit.AddLesson("C");

        unit.RemoveLesson(middle);

        unit.Lessons.Should().HaveCount(2);
        unit.Lessons.Select(l => l.Position).Should().Equal(1, 2);
    }

    [Fact]
    public void RemoveLesson_FromAnotherUnit_Throws()
    {
        var unit = NewUnit();
        var otherUnit = NewUnit();
        var other = otherUnit.AddLesson("Foreign");

        var act = () => unit.RemoveLesson(other);

        act.Should().Throw<DomainException>();
    }
}