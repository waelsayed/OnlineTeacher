namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// A teacher followed by a student, presented as the browseable public platform(s) the
/// teacher owns (publicId + slug). Never exposes internal database identifiers.
/// </summary>
public sealed record FollowedTeacher(string PublicId, string Slug);