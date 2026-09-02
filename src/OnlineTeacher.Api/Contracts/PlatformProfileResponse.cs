using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Response for management reads of the resolved Teacher Platform profile
/// (<c>GET /{publicId}/{slug}/api/platform/profile</c>). The internal Id and stable
/// PublicId are read-only; only Name and Slug are editable.
/// </summary>
public sealed record PlatformProfileResponse(
    Guid PlatformId,
    string PublicId,
    string Name,
    string Slug,
    string Status)
{
    public static PlatformProfileResponse From(OnlineTeacher.Application.Dtos.PlatformProfileResult result) =>
        new(
            result.PlatformId,
            result.PublicId,
            result.Name,
            result.Slug,
            result.Status switch
            {
                PlatformStatus.PendingActivation => "PendingActivation",
                PlatformStatus.Active => "Active",
                PlatformStatus.Deactivated => "Deactivated",
                _ => result.Status.ToString()
            });
}