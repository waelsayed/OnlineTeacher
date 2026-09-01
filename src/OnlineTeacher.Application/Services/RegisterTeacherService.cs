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
/// Registers a teacher with a central identity.
/// The central platform owns teacher accounts; a teacher belongs centrally, not to a tenant.
/// </summary>
public sealed class RegisterTeacherService
{
    private readonly ITeacherRepository _teachers;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public RegisterTeacherService(
        ITeacherRepository teachers,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _teachers = teachers;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<TeacherRegistrationResult> RegisterAsync(
        string? name,
        string? email,
        string? password,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Password is required.");
        }

        var teacher = CreateTeacher(name, email);
        teacher.SetPasswordHash(_passwordHasher.Hash(password));

        _teachers.Add(teacher);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new TeacherRegistrationResult(teacher.Id);
    }

    private static Teacher CreateTeacher(string? name, string? email)
    {
        try
        {
            return new Teacher(name ?? string.Empty, Email.Create(email ?? string.Empty));
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }
}