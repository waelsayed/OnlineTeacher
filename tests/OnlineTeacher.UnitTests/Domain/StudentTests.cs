using FluentAssertions;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Domain;

public class StudentTests
{
    private static Student NewStudent()
    {
        var student = new Student("Ahmed Hassan", Email.Create("ahmed@example.com"));
        student.SetPasswordHash("hashed-password");
        return student;
    }

    [Fact]
    public void Create_SetsNameAndEmail()
    {
        var student = new Student("Ahmed Hassan", Email.Create("ahmed@example.com"));

        student.Name.Should().Be("Ahmed Hassan");
        student.Email.Value.Should().Be("ahmed@example.com");
    }

    [Fact]
    public void Create_RejectsEmptyName()
    {
        var act = () => new Student("  ", Email.Create("ahmed@example.com"));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_AssignsNewId()
    {
        var student = NewStudent();

        student.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void SetPasswordHash_StoresHash()
    {
        var student = NewStudent();

        student.PasswordHash.Should().Be("hashed-password");
    }

    [Fact]
    public void SetPasswordHash_RejectsEmptyHash()
    {
        var student = NewStudent();

        var act = () => student.SetPasswordHash(" ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddFollow_AddsFollow()
    {
        var student = NewStudent();
        var follow = new StudentFollow(student.Id, Guid.NewGuid());

        student.AddFollow(follow);

        student.Follows.Should().ContainSingle();
    }

    [Fact]
    public void AddFollow_RejectsFollowForAnotherStudent()
    {
        var student = NewStudent();
        var follow = new StudentFollow(Guid.NewGuid(), Guid.NewGuid());

        var act = () => student.AddFollow(follow);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddFollow_RejectsDuplicateTeacher()
    {
        var student = NewStudent();
        var teacherId = Guid.NewGuid();
        student.AddFollow(new StudentFollow(student.Id, teacherId));

        var act = () => student.AddFollow(new StudentFollow(student.Id, teacherId));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void RemoveFollow_RemovesFollow()
    {
        var student = NewStudent();
        var follow = new StudentFollow(student.Id, Guid.NewGuid());
        student.AddFollow(follow);

        student.RemoveFollow(follow);

        student.Follows.Should().BeEmpty();
    }

    [Fact]
    public void RemoveFollow_RejectsFollowForAnotherStudent()
    {
        var student = NewStudent();
        var other = new StudentFollow(Guid.NewGuid(), Guid.NewGuid());

        var act = () => student.RemoveFollow(other);

        act.Should().Throw<DomainException>();
    }
}