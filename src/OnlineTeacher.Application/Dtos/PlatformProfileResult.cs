using OnlineTeacher.Domain.Enums;

namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// Management-facing projection of a Teacher Platform's editable profile.
/// The internal Id and stable PublicId are immutable and never changeable through a
/// normal management API; only Name and Slug are editable.
/// </summary>
public sealed record PlatformProfileResult(
    Guid PlatformId,
    string PublicId,
    string Name,
    string Slug,
    PlatformStatus Status);