using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OnlineTeacher.Application.Dtos;

namespace OnlineTeacher.Api.Authentication;

/// <summary>
/// Issues platform-scoped JWT bearer tokens at the API boundary from an authenticated
/// teacher. Claims carry only identity/authorization data (sub = TeacherId, tenant =
/// public id, roles and permission codes); no password or hash material is included.
/// Backed by Microsoft's token handler and configuration-driven signing credentials.
/// </summary>
public sealed class JwtTokenFactory
{
    private readonly JwtOptions _options;

    public JwtTokenFactory(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string Create(TeacherPlatformAccess access)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey))
            {
                KeyId = _options.KeyId
            },
            SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, access.TeacherId.ToString()),
            new(JwtClaims.Tenant, access.PublicId),
            new(JwtClaims.IsOwner, access.IsOwner ? "true" : "false"),
            new(JwtClaims.PrincipalType, PrincipalTypes.Teacher)
        };

        foreach (var role in access.RoleNames)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        foreach (var permission in access.PermissionCodes)
        {
            claims.Add(new Claim(PermissionClaims.Type, permission));
        }

        return WriteToken(claims);
    }

    /// <summary>
    /// Issues a central, tenant-agnostic student JWT. A student token carries no platform
    /// tenant claim and no permission/role claims, so it can never satisfy Teacher-only
    /// platform-management authorization. The principal type distinguishes it from a teacher.
    /// </summary>
    public string CreateStudent(Guid studentId)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, studentId.ToString()),
            new(JwtClaims.PrincipalType, PrincipalTypes.Student)
        };

        return WriteToken(claims);
    }

    private string WriteToken(IEnumerable<Claim> claims)
    {
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey))
            {
                KeyId = _options.KeyId
            },
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(_options.TokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}