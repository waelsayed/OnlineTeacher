namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// A teacher the current student follows, presented as the browseable public platform(s) the
/// teacher owns. Never exposes internal database identifiers.
/// </summary>
public sealed record FollowedTeacherResponse(string PublicId, string Slug);