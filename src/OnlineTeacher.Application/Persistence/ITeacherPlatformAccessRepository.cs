using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Application.Persistence;

/// <summary>
/// Purpose-specific read of a teacher's access within one tenant
/// (membership, roles, and granted permission codes).
/// </summary>
public interface ITeacherPlatformAccessRepository
{
    Task<TeacherPlatformAccess?> GetAsync(Guid teacherId, Guid tenantId, CancellationToken cancellationToken = default);
}