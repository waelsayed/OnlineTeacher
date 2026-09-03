using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class CourseTests
{
    private static Course NewCourse(string title = "Algebra") =>
        new(Guid.NewGuid(), title, "An algebra course.");

    [Fact]
    public void Create_DefaultsToDraftAndOpensWithNoUnits()
    {
        var course = NewCourse();

        course.Status.Should().Be(CourseStatus.Draft);
        course.Units.Should().BeEmpty();
    }

    [Fact]
    public void Create_RequiresTenant()
    {
        var act = () => new Course(Guid.Empty, "Algebra");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresTitle()
    {
        var act = () => new Course(Guid.NewGuid(), "   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_TrimsTitleAndBlankSummaryBecomesNull()
    {
        var course = new Course(Guid.NewGuid(), "  Algebra  ", "   ");

        course.Title.Should().Be("Algebra");
        course.Summary.Should().BeNull();
    }

    [Fact]
    public void Publish_TransitionsDraftToPublished()
    {
        var course = NewCourse();

        course.Publish();

        course.Status.Should().Be(CourseStatus.Published);
    }

    [Fact]
    public void Publish_WhenAlreadyPublished_Throws()
    {
        var course = NewCourse();
        course.Publish();

        var act = course.Publish;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ToDraft_TransitionsPublishedToDraft()
    {
        var course = NewCourse();
        course.Publish();

        course.ToDraft();

        course.Status.Should().Be(CourseStatus.Draft);
    }

    [Fact]
    public void ToDraft_WhenAlreadyDraft_Throws()
    {
        var course = NewCourse();

        var act = course.ToDraft;

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Update_OnlyChangesSuppliedFields()
    {
        var course = NewCourse();

        course.Update("Geometry", null);

        course.Title.Should().Be("Geometry");
        course.Summary.Should().Be("An algebra course.");
    }

    [Fact]
    public void Update_WithBlankTitle_Throws()
    {
        var course = NewCourse();

        var act = () => course.Update("   ", null);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddUnit_AppendsAtNextPosition()
    {
        var course = NewCourse();

        course.AddUnit("Unit One");
        course.AddUnit("Unit Two");

        course.Units.Should().HaveCount(2);
        course.Units[0].Position.Should().Be(1);
        course.Units[1].Position.Should().Be(2);
    }

    [Fact]
    public void AddUnit_AtExplicitPosition_ShiftsLaterUnits()
    {
        var course = NewCourse();
        course.AddUnit("First");
        course.AddUnit("Second");
        var third = course.AddUnit("Third");

        var inserted = course.AddUnit("Inserted", 2);

        inserted.Position.Should().Be(2);
        third.Position.Should().Be(4);
        course.Units.Select(u => u.Position).Should().Equal(1, 2, 3, 4);
    }

    [Fact]
    public void AddUnit_WithBlankTitle_Throws()
    {
        var course = NewCourse();

        var act = () => course.AddUnit("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddUnit_WithNonPositivePosition_Throws()
    {
        var course = NewCourse();

        var act = () => course.AddUnit("Unit", 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveUnit_KeepsPositionsContiguous()
    {
        var course = NewCourse();
        course.AddUnit("A");
        course.AddUnit("B");
        course.AddUnit("C");
        var target = course.Units[0];

        course.MoveUnit(target, 3);

        course.Units.Select(u => u.Position).Should().Equal(1, 2, 3);
        course.Units.Single(u => u.Id == target.Id).Position.Should().Be(3);
    }

    [Fact]
    public void MoveUnit_OutOfRange_Throws()
    {
        var course = NewCourse();
        course.AddUnit("A");
        var target = course.Units[0];

        var act = () => course.MoveUnit(target, 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoveUnit_ReindexesRemainingUnits()
    {
        var course = NewCourse();
        course.AddUnit("A");
        var middle = course.AddUnit("B");
        course.AddUnit("C");

        course.RemoveUnit(middle);

        course.Units.Should().HaveCount(2);
        course.Units.Select(u => u.Position).Should().Equal(1, 2);
    }

    [Fact]
    public void RemoveUnit_FromAnotherCourse_Throws()
    {
        var course = NewCourse();
        var otherCourse = NewCourse();
        var other = otherCourse.AddUnit("Foreign");

        var act = () => course.RemoveUnit(other);

        act.Should().Throw<DomainException>();
    }
}