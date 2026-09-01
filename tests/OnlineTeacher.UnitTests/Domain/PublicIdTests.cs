using FluentAssertions;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Domain;

public class PublicIdTests
{
    [Fact]
    public void Generate_ReturnsTwelveCharacters()
    {
        var id = PublicId.Generate();

        id.Value.Should().HaveLength(12);
    }

    [Fact]
    public void Generate_ProducesOnlyUrlSafeAlphanumericCharacters()
    {
        var id = PublicId.Generate();

        id.Value.Should().MatchRegex("^[0-9A-Za-z]{12}$");
    }

    [Fact]
    public void Generate_ProducesDistinctNonSequentialValues()
    {
        var ids = Enumerable.Range(0, 100).Select(_ => PublicId.Generate().Value).ToHashSet();

        ids.Should().HaveCount(100);
    }

    [Fact]
    public void Create_AcceptsGeneratedValue()
    {
        var generated = PublicId.Generate().Value;

        var id = PublicId.Create(generated);

        id.Value.Should().Be(generated);
    }

    [Fact]
    public void GeneratedValues_AreEqualWhenValuesMatch()
    {
        var value = PublicId.Generate().Value;

        var first = PublicId.Create(value);
        var second = PublicId.Create(value);

        first.Should().Be(second);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("short")]
    [InlineData("ThisStringIsWayTooLongForPublicId")]
    [InlineData("abc123def!!")]
    public void Create_RejectsInvalidValue(string value)
    {
        var act = () => PublicId.Create(value);

        act.Should().Throw<DomainException>();
    }
}