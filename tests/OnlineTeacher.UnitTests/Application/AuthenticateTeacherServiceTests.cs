using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class AuthenticateTeacherServiceTests
{
    private readonly FakeTeacherRepository _teachers = new();
    private readonly StubPasswordHasher _passwordHasher = new();
    private readonly StubTenantContext _tenantContext = new();

    private AuthenticateTeacherService CreateService() =>
        new(_teachers, _passwordHasher, _tenantContext);

    private Teacher SeedTeacher(string email = "wael@example.com", string password = "secret")
    {
        var teacher = new Teacher("Wael Sayed", Email.Create(email));
        teacher.SetPasswordHash(_passwordHasher.Hash(password));
        _teachers.Seed(teacher);
        return teacher;
    }

    [Fact]
    public async Task Authenticate_ValidCredentials_Succeeds()
    {
        var teacher = SeedTeacher();
        var service = CreateService();

        var result = await service.AuthenticateAsync("wael@example.com", "secret");

        result.Succeeded.Should().BeTrue();
        result.TeacherId.Should().Be(teacher.Id);
        result.FailureMessage.Should().BeNull();
    }

    [Fact]
    public async Task Authenticate_UnknownEmail_FailsGenerically()
    {
        SeedTeacher();
        var service = CreateService();

        var result = await service.AuthenticateAsync("nobody@example.com", "secret");

        result.Succeeded.Should().BeFalse();
        result.TeacherId.Should().BeNull();
        result.FailureMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Authenticate_WrongPassword_FailsGenerically()
    {
        SeedTeacher();
        var service = CreateService();

        var result = await service.AuthenticateAsync("wael@example.com", "wrong-password");

        result.Succeeded.Should().BeFalse();
        result.TeacherId.Should().BeNull();
    }

    [Fact]
    public async Task Authenticate_UnknownEmailAndWrongPassword_FailWithSameMessage()
    {
        SeedTeacher();
        var service = CreateService();

        var unknownEmail = await service.AuthenticateAsync("nobody@example.com", "wrong-password");
        var wrongPassword = await service.AuthenticateAsync("wael@example.com", "wrong-password");

        unknownEmail.FailureMessage.Should().Be(wrongPassword.FailureMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public async Task Authenticate_EmptyEmail_ThrowsValidationException(string? email)
    {
        var service = CreateService();

        var act = () => service.AuthenticateAsync(email, "secret");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Authenticate_EmptyPassword_ThrowsValidationException()
    {
        var service = CreateService();

        var act = () => service.AuthenticateAsync("wael@example.com", " ");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Authenticate_InvalidEmailFormat_ThrowsValidationException()
    {
        var service = CreateService();

        var act = () => service.AuthenticateAsync("not-an-email", "secret");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Authenticate_UnderTeacherTenantContext_ThrowsTenantMismatch()
    {
        SeedTeacher();
        _tenantContext.TrySetTenant(Guid.NewGuid());
        var service = CreateService();

        var act = () => service.AuthenticateAsync("wael@example.com", "secret");

        await act.Should().ThrowAsync<TenantMismatchException>();
    }

    [Fact]
    public async Task Authenticate_ResultNeverExposesPasswordHash()
    {
        SeedTeacher();
        var service = CreateService();

        var failure = await service.AuthenticateAsync("wael@example.com", "wrong-password");

        failure.FailureMessage.Should().NotContain("secret");
        failure.FailureMessage.Should().NotContain("hash");
    }
}