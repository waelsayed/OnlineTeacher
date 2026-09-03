using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Application.Tenancy;
using OnlineTeacher.Domain.Exceptions;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Lists a central Student's enrollments scoped to a single Teacher Platform. The target
/// platform is addressed by publicId; the tenant context is scoped for the tenant-scoped
/// enrollment read and restored afterwards. The list is ordered by enrollment date.
/// </summary>
public sealed class ListStudentEnrollmentsService
{
    private readonly IPlatformRepository _platforms;
    private readonly IStudentRepository _students;
    private readonly IEnrollmentRepository _enrollments;
    private readonly ITenantContext _tenantContext;

    public ListStudentEnrollmentsService(
        IPlatformRepository platforms,
        IStudentRepository students,
        IEnrollmentRepository enrollments,
        ITenantContext tenantContext)
    {
        _platforms = platforms;
        _students = students;
        _enrollments = enrollments;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<EnrollmentListItem>> ListAsync(
        Guid studentId,
        string? teacherPublicId,
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

            return await _enrollments.ListByStudentForPlatformAsync(studentId, platform.Id, cancellationToken);
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
