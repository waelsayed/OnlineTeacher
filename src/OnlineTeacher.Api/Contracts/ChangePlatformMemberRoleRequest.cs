namespace OnlineTeacher.Api.Contracts;

/// <summary>
/// Request body for <c>PUT/PATCH /{publicId}/{slug}/api/platform/members/{teacherId}</c>.
/// The role name must already exist within the tenant (e.g. "Owner", "Assistant").
/// </summary>
public sealed record ChangePlatformMemberRoleRequest(string RoleName);