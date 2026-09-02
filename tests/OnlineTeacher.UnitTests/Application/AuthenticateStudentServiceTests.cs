using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.ValueObjects;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class AuthenticateStudentServiceTests
{
    private readonly FakeStudentRepository _students = new();
    private readonly StubPasswordHasher _hasher = new();
    private readonly StubTenantContext _tenantContext = new();

    private AuthenticateStudentService CreateService() => new(_students, _hasher, _tenantContext);

    private static Student SeedStudent(FakeStudentRepository students, string email = "sara@example.com")
    {
        var student = new Student("Sara", Email.Create(email));
        student.SetPasswordHash("hashed:s3cret");
        students.Seed(student);
        return student;
    }

    [Fact]
    public async Task Authenticate_ValidCredentials_ReturnsOk()
    {
        var student = SeedStudent(_students);

        var result = await CreateService().AuthenticateAsync(student.Email.Value, "s3cret");

        result.Succeeded.Should().BeTrue();
        result.StudentId.Should().Be(student.Id);
    }

    [Fact]
    public async Task Authenticate_WrongPassword_ReturnsFailed()
    {
        var student = SeedStudent(_students);

        var result = await CreateService().AuthenticateAsync(student.Email.Value, "wrong");

        result.Succeeded.Should().BeFalse();
        result.StudentId.Should().BeNull();
    }

    [Fact]
    public async Task Authenticate_UnknownEmail_ReturnsFailedGeneric()
    {
        var result = await CreateService().AuthenticateAsync("nobody@example.com", "s3cret");

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Authenticate_MissingInput_ThrowsValidation()
    {
        var act = () => CreateService().AuthenticateAsync("  ", "s3cret");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Authenticate_FailureIsGeneric_DoesNotRevealExistence()
    {
        var student = SeedStudent(_students);

        var wrongPassword = await CreateService().AuthenticateAsync(student.Email.Value, "wrong");
        var unknownEmail = await CreateService().AuthenticateAsync("nobody@example.com", "s3cret");

        wrongPassword.FailureMessage.Should().Be(unknownEmail.FailureMessage);
    }
}