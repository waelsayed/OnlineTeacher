using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace OnlineTeacher.Api.Authorization;

/// <summary>
/// Resolves permission policies on demand so permission-specific policies (e.g.
/// "Permission:Platform.Access") do not have to be declared up front. Falls back to the
/// default policy provider for everything else.
/// </summary>
public sealed class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly IAuthorizationPolicyProvider _defaultProvider;

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    {
        _defaultProvider = new DefaultAuthorizationPolicyProvider(options);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _defaultProvider.GetPolicyAsync(policyName);
        }

        var permission = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
        var requirement = new PermissionRequirement(permission);
        var policy = new AuthorizationPolicyBuilder(Array.Empty<string>())
            .AddRequirements(requirement)
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _defaultProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _defaultProvider.GetFallbackPolicyAsync();
}