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
/// Registers a student with one central identity. The student belongs centrally, not to any
/// Teacher Platform tenant. Duplicate email is rejected via the database unique constraint and
/// translated by the persistence layer; no application-side existence pre-check creates a race.
/// </summary>
public sealed class RegisterStudentService
{
    private readonly IStudentRepository _students;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public RegisterStudentService(
        IStudentRepository students,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _students = students;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<StudentRegistrationResult> RegisterAsync(
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

        var student = CreateStudent(name, email);
        student.SetPasswordHash(_passwordHasher.Hash(password));

        _students.Add(student);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new StudentRegistrationResult(student.Id);
    }

    private static Student CreateStudent(string? name, string? email)
    {
        try
        {
            return new Student(name ?? string.Empty, Email.Create(email ?? string.Empty));
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }
}