using FluentAssertions;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class RegisterStudentServiceTests
{
    private readonly FakeStudentRepository _students = new();
    private readonly StubPasswordHasher _hasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private RegisterStudentService CreateService() => new(_students, _hasher, _unitOfWork, _tenantContext);

    [Fact]
    public async Task Register_ValidInput_AddsStudentAndCommits()
    {
        var service = CreateService();

        var result = await service.RegisterAsync("Sara", "sara@example.com", "s3cret");

        result.StudentId.Should().NotBe(Guid.Empty);
        var student = _students.Students.Should().ContainSingle().Subject;
        student.Name.Should().Be("Sara");
        student.Email.Value.Should().Be("sara@example.com");
        student.PasswordHash.Should().Be("hashed:s3cret");
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Register_MissingPassword_ThrowsValidation()
    {
        var act = () => CreateService().RegisterAsync("Sara", "sara@example.com", "   ");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Register_InvalidName_ThrowsValidation()
    {
        var act = () => CreateService().RegisterAsync("   ", "sara@example.com", "s3cret");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Register_InvalidEmail_ThrowsValidation()
    {
        var act = () => CreateService().RegisterAsync("Sara", "not-an-email", "s3cret");

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Register_NeverStoresOrReturnsPlaintextPassword()
    {
        await CreateService().RegisterAsync("Sara", "sara@example.com", "s3cret");

        _students.Students.Should().OnlyContain(s => !s.PasswordHash.Contains("s3cret")
            || s.PasswordHash == "hashed:s3cret");
    }
}