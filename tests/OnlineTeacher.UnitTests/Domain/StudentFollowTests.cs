using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.UnitTests.Domain;

public class StudentFollowTests
{
    [Fact]
    public void Create_SetsIds()
    {
        var studentId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        var follow = new StudentFollow(studentId, teacherId);

        follow.StudentId.Should().Be(studentId);
        follow.TeacherId.Should().Be(teacherId);
    }

    [Fact]
    public void Create_RejectsEmptyStudentId()
    {
        var act = () => new StudentFollow(Guid.Empty, Guid.NewGuid());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsEmptyTeacherId()
    {
        var act = () => new StudentFollow(Guid.NewGuid(), Guid.Empty);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsSelfFollow()
    {
        var id = Guid.NewGuid();

        var act = () => new StudentFollow(id, id);

        act.Should().Throw<DomainException>();
    }
}