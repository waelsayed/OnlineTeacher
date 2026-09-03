using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class LessonTests
{
    [Fact]
    public void Create_SetsPositionAndSnapshotsUnitCourseTenant()
    {
        var unitId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var lesson = new Lesson(unitId, courseId, tenantId, "Lesson One", 4);

        lesson.UnitId.Should().Be(unitId);
        lesson.CourseId.Should().Be(courseId);
        lesson.TenantId.Should().Be(tenantId);
        lesson.Position.Should().Be(4);
    }

    [Fact]
    public void Create_RequiresUnit()
    {
        var act = () => new Lesson(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "Lesson One", 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresCourse()
    {
        var act = () => new Lesson(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "Lesson One", 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresTenant()
    {
        var act = () => new Lesson(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, "Lesson One", 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresTitle()
    {
        var act = () => new Lesson(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "   ", 1);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RequiresPositivePosition()
    {
        var act = () => new Lesson(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Lesson One", 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Rename_TrimsTitle()
    {
        var lesson = NewLesson();

        lesson.Rename("  Updated  ");

        lesson.Title.Should().Be("Updated");
    }

    [Fact]
    public void Rename_WithBlankTitle_Throws()
    {
        var lesson = NewLesson();

        var act = () => lesson.Rename("   ");

        act.Should().Throw<DomainException>();
    }

    private static Lesson NewLesson() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Lesson One", 1);
}