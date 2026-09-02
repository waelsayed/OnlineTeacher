namespace OnlineTeacher.Api.Authentication;

/// <summary>
/// JWT signing and lifetime configuration, bound from the file-based "Jwt" section and
/// environment overrides. Secrets come from configuration/environment only, never hard-coded
/// constants. Development defaults are inert placeholders.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int TokenLifetimeMinutes { get; set; } = 120;

    /// <summary>
    /// Logical key identifier emitted in the token's <c>kid</c> header. IdentityModel 8+
    /// requires a non-empty <c>kid</c> when validating signatures; keeping it stable and
    /// configured (not derived from the secret) lets the API and issuers agree.
    /// </summary>
    public string KeyId { get; set; } = "OnlineTeacher.SigningKey";
}