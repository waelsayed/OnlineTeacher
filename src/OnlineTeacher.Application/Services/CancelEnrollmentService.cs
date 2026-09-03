using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Cancels a central Student's own enrollment in a Teacher Platform course. The target
/// platform is addressed by publicId. The tenant context is scoped for the tenant-scoped
/// enrollment lookup and restored afterwards. Only the enrollment owner can cancel it.
/// </summary>
public sealed class CancelEnrollmentService
{
    private readonly IPlatformRepository _platforms;
    private readonly IStudentRepository _students;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public CancelEnrollmentService(
        IPlatformRepository platforms,
        IStudentRepository students,
        IEnrollmentRepository enrollments,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _students = students;
        _enrollments = enrollments;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task CancelAsync(
        Guid studentId,
        string? teacherPublicId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);

        var currentTenant = _tenantContext.TenantId;

        try
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.TrySetTenant(platform.Id);
            }

            var enrollment = await _enrollments.GetAsync(studentId, courseId, cancellationToken)
                ?? throw new NotFoundException("Enrollment does not exist.");

            if (enrollment.StudentId != studentId)
            {
                throw new NotFoundException("Enrollment does not exist.");
            }

            try
            {
                enrollment.Cancel();
            }
            catch (DomainException exception)
            {
                throw new BusinessRuleViolationException(exception.Message, exception);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.Clear();
            }
        }
    }
}
