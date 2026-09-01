using FluentAssertions;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Domain;

public class EmailTests
{
    [Theory]
    [InlineData("teacher@example.com")]
    [InlineData("a.b@sub.example.org")]
    [InlineData("teacher+tag@example.co")]
    public void Create_AcceptsValidEmail(string value)
    {
        var email = Email.Create(value);

        email.Value.Should().Be(value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("not-an-email")]
    [InlineData("missing@dot")]
    [InlineData("@nodomain.com")]
    [InlineData("a b@example.com")]
    public void Create_RejectsInvalidEmail(string value)
    {
        var act = () => Email.Create(value);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_TrimsSurroundingWhitespace()
    {
        var email = Email.Create("  teacher@example.com  ");

        email.Value.Should().Be("teacher@example.com");
    }
}