using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Enums;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Enrolls a central Student into a tenant-scoped Course. The student JWT is central (no tenant
/// claims); the target platform is addressed by publicId and the tenant context is scoped for the
/// tenant-scoped Course read, then restored. Only Published courses in an Active platform are
/// eligible. Duplicate (Student, Course) enrollments are rejected and the database unique
/// constraint guarantees the pair cannot repeat.
/// </summary>
public sealed class EnrollStudentService
{
    private readonly IPlatformRepository _platforms;
    private readonly IStudentRepository _students;
    private readonly ICourseRepository _courses;
    private readonly IEnrollmentRepository _enrollments;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;

    public EnrollStudentService(
        IPlatformRepository platforms,
        IStudentRepository students,
        ICourseRepository courses,
        IEnrollmentRepository enrollments,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _students = students;
        _courses = courses;
        _enrollments = enrollments;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
    }

    public async Task<Guid> EnrollAsync(
        Guid studentId,
        string? teacherPublicId,
        Guid courseId,
        CancellationToken cancellationToken = default)
    {
        TenantContextGuard.EnsureCentral(_tenantContext);

        var student = await _students.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException("Student does not exist.");

        var platform = await PlatformResolver.ResolveAsync(_platforms, teacherPublicId, cancellationToken);

        if (platform.Status != PlatformStatus.Active)
        {
            throw new BusinessRuleViolationException("The teacher platform is not active.");
        }

        var currentTenant = _tenantContext.TenantId;

        try
        {
            if (!currentTenant.HasValue)
            {
                _tenantContext.TrySetTenant(platform.Id);
            }

            var course = await _courses.GetByIdAsync(platform.Id, courseId, cancellationToken)
                ?? throw new NotFoundException("Course does not exist.");

            if (course.Status != CourseStatus.Published)
            {
                throw new BusinessRuleViolationException("Only published courses can be enrolled in.");
            }

            if (await _enrollments.GetAsync(studentId, courseId, cancellationToken) is not null)
            {
                throw new BusinessRuleViolationException("The student is already enrolled in this course.");
            }

            var enrollment = new Enrollment(studentId, courseId, platform.Id);

            _enrollments.Add(enrollment);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return enrollment.Id;
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
