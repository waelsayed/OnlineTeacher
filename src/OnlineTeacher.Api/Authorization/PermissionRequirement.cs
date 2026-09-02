using Microsoft.AspNetCore.Authorization;

namespace OnlineTeacher.Api.Authorization;

/// <summary>
/// The permission a user must possess to satisfy the policy. The handler only trusts
/// permission claims issued by the server inside the token (handled by the JWT bearer
/// scheme), never anything supplied by the client.
/// </summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;