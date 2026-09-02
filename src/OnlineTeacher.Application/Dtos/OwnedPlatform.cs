namespace OnlineTeacher.Application.Dtos;

/// <summary>
/// The public identity of a Teacher Platform owned by a teacher. Used to present a followed
/// teacher as a browseable public platform (publicId + slug) without exposing internal Ids.
/// </summary>
public sealed record OwnedPlatform(string PublicId, string Slug);