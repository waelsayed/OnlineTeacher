using OnlineTeacher.Application.Exceptions;
using OnlineTeacher.Application.Persistence;
using OnlineTeacher.Domain.Entities;
using OnlineTeacher.Domain.Exceptions;
using OnlineTeacher.Domain.ValueObjects;

namespace OnlineTeacher.Application.Services;

/// <summary>
/// Resolves a <see cref="TeacherPlatform"/> from a route publicId, translating an invalid
/// publicId into a validation error and an unknown publicId into a not-found error.
/// Shared by the management use cases.
/// </summary>
internal static class PlatformResolver
{
    public static async Task<TeacherPlatform> ResolveAsync(
        IPlatformRepository platforms,
        string? publicId,
        CancellationToken cancellationToken)
    {
        try
        {
            return await platforms.GetByPublicIdAsync(PublicId.Create(publicId ?? string.Empty), cancellationToken)
                ?? throw new NotFoundException("Teacher platform does not exist.");
        }
        catch (DomainException exception)
        {
            throw new ValidationException(exception.Message, exception);
        }
    }
}