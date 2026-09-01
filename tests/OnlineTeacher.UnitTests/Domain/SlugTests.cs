using FluentAssertions;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.UnitTests.Domain;

public class SlugTests
{
    [Fact]
    public void CreateFromName_LowercasesAndNormalizesSymbols()
    {
        var slug = Slug.CreateFromName("Ahmed's Class");

        slug.Value.Should().Be("ahmed-s-class");
    }

    [Fact]
    public void CreateFromName_CollapsesMultipleSeparators()
    {
        var slug = Slug.CreateFromName("  Hello   World  ");

        slug.Value.Should().Be("hello-world");
    }

    [Fact]
    public void CreateFromName_TrimsLeadingAndTrailingHyphens()
    {
        var slug = Slug.CreateFromName("---hello-world---");

        slug.Value.Should().Be("hello-world");
    }

    [Fact]
    public void CreateFromName_IsDeterministic()
    {
        var first = Slug.CreateFromName("My Platform");
        var second = Slug.CreateFromName("My Platform");

        first.Should().Be(second);
        first.Value.Should().Be("my-platform");
    }

    [Fact]
    public void CreateFromName_FallsBackWhenNoUrlSafeCharactersPresent()
    {
        var slug = Slug.CreateFromName("محمد رياضيات");

        slug.Value.Should().Be("platform");
    }

    [Fact]
    public void CreateFromName_RejectsNullOrWhitespaceName()
    {
        var act = () => Slug.CreateFromName("   ");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_AcceptsCanonicalSlug()
    {
        var slug = Slug.Create("my-canonical-slug-2026");

        slug.Value.Should().Be("my-canonical-slug-2026");
    }

    [Theory]
    [InlineData("Hello")]
    [InlineData("hello--world")]
    [InlineData("-hello")]
    [InlineData("hello-")]
    [InlineData("hello world")]
    [InlineData("héllo")]
    public void Create_RejectsNonCanonicalSlug(string value)
    {
        var act = () => Slug.Create(value);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_RejectsSlugLongerThanMaxLength()
    {
        var tooLong = new string('a', 61);

        var act = () => Slug.Create(tooLong);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_AcceptsValueProducedByNormalization()
    {
        var normalized = Slug.CreateFromName("Ahmed's Class").Value;

        var slug = Slug.Create(normalized);

        slug.Value.Should().Be(normalized);
    }
}