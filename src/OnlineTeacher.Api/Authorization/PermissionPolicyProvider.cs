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
        if (policyName.StartsWith(RequirePermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[RequirePermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder(Array.Empty<string>())
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        if (policyName.StartsWith(RequirePrincipalTypeAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            var principalType = policyName[RequirePrincipalTypeAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder(Array.Empty<string>())
                .AddRequirements(new PrincipalTypeRequirement(principalType))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _defaultProvider.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _defaultProvider.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _defaultProvider.GetFallbackPolicyAsync();
}