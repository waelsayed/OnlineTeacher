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
/// Authenticates a central teacher by email and password.
/// Both unknown email and wrong password fail with the same generic result so the stored
/// hash and email existence are never revealed. JWT generation belongs to a later layer.
/// </summary>
public sealed class AuthenticateTeacherService
{
    private readonly ITeacherRepository _teachers;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;

    public AuthenticateTeacherService(
        ITeacherRepository teachers,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext)
    {
        _teachers = teachers;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Email and password are required.");
        }

        var teacher = await FindByEmail(email, cancellationToken);

        if (teacher is null || !_passwordHasher.Verify(password, teacher.PasswordHash))
        {
            return AuthenticationResult.Failed;
        }

        return AuthenticationResult.Ok(teacher.Id);
    }

    private async Task<Teacher?> FindByEmail(string email, CancellationToken cancellationToken)
    {
        try
        {
            return await _teachers.GetByEmailAsync(Email.Create(email), cancellationToken);
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }
}