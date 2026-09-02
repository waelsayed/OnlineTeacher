using OnlineTeacher.Application.Dtos;
using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Security;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Authenticates a central student by email and password. Student identity is central, so no
/// Teacher Platform PublicId is required. Both unknown email and wrong password fail with the
/// same generic result so the stored hash and email existence are never revealed.
/// </summary>
public sealed class AuthenticateStudentService
{
    private readonly IStudentRepository _students;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;

    public AuthenticateStudentService(
        IStudentRepository students,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext)
    {
        _students = students;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
    }

    public async Task<StudentAuthenticationResult> AuthenticateAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Email and password are required.");
        }

        var student = await FindByEmail(email, cancellationToken);

        if (student is null || !_passwordHasher.Verify(password, student.PasswordHash))
        {
            return StudentAuthenticationResult.Failed;
        }

        return StudentAuthenticationResult.Ok(student.Id);
    }

    private async Task<Student?> FindByEmail(string email, CancellationToken cancellationToken)
    {
        try
        {
            return await _students.GetByEmailAsync(Email.Create(email), cancellationToken);
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }
}