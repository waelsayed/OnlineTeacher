namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>POST /api/auth/login</c>. A platform-scoped JWT is issued, so the
/// caller must supply the Teacher Platform <c>publicId</c> to select the tenant.
/// </summary>
public sealed record LoginRequest(string? Email, string? Password, string? PublicId);