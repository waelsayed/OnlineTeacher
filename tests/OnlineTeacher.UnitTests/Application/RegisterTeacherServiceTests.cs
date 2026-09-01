using FluentAssertions;
using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Services;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.UnitTests.Application.TestDoubles;

namespace OnlineTeacher.UnitTests.Application;

public class RegisterTeacherServiceTests
{
    private readonly FakeTeacherRepository _teachers = new();
    private readonly StubPasswordHasher _passwordHasher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly StubTenantContext _tenantContext = new();

    private RegisterTeacherService CreateService() =>
        new(_teachers, _passwordHasher, _unitOfWork, _tenantContext);

    [Fact]
    public async Task Register_ValidInput_CreatesTeacherWithHashedPassword()
    {
        var service = CreateService();

        var result = await service.RegisterAsync("Wael Sayed", "wael@example.com", "secret");

        _teachers.Teachers.Should().ContainSingle();
        var teacher = _teachers.Teachers.Single();
        result.TeacherId.Should().Be(teacher.Id);
        teacher.Name.Should().Be("Wael Sayed");
        teacher.Email.Value.Should().Be("wael@example.com");
        _unitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task Register_DoesNotPersistPlaintextPassword()
    {
        var service = CreateService();

        await service.RegisterAsync("Wael Sayed", "wael@example.com", "secret");

        var teacher = _teachers.Teachers.Single();
        teacher.PasswordHash.Should().NotBe("secret");
        teacher.PasswordHash.Should().NotBeNullOrWhiteSpace();
        teacher.PasswordHash.Should().Be("hashed:secret");
    }

    [Fact]
    public void Register_EmptyName_ThrowsValidationException()
    {
        var service = CreateService();

        var act = () => service.RegisterAsync("  ", "wael@example.com", "secret");

        act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    [InlineData(null)]
    public void Register_InvalidEmail_ThrowsValidationException(string? email)
    {
        var service = CreateService();

        var act = () => service.RegisterAsync("Wael Sayed", email, "secret");

        act.Should().ThrowAsync<ValidationException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Register_EmptyPassword_ThrowsValidationException(string? password)
    {
        var service = CreateService();

        var act = () => service.RegisterAsync("Wael Sayed", "wael@example.com", password);

        act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Register_DuplicateEmail_PropagatesCleanConflict()
    {
        var duplicateUnitOfWork = new FakeUnitOfWork(
            onSave: () => throw new DuplicateEmailException());
        var service = new RegisterTeacherService(_teachers, _passwordHasher, duplicateUnitOfWork, _tenantContext);

        var act = () => service.RegisterAsync("Wael Sayed", "wael@example.com", "secret");

        await act.Should().ThrowAsync<DuplicateEmailException>();
    }

    [Fact]
    public async Task Register_UnderTeacherTenantContext_ThrowsTenantMismatch()
    {
        _tenantContext.TrySetTenant(Guid.NewGuid());
        var service = CreateService();

        var act = () => service.RegisterAsync("Wael Sayed", "wael@example.com", "secret");

        await act.Should().ThrowAsync<TenantMismatchException>();
        _teachers.Teachers.Should().BeEmpty();
    }
}