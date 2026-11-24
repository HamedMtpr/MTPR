using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Mtpr.LocalTokenService;

public sealed class JwtTokenIssuer(ISigningKeyProvider keys) : ITokenIssuer
{
    public string Issue(LocalClient client, LocalTokenOptions options)
    {
        var now = DateTime.UtcNow;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, client.Subject)
        };
        claims.AddRange(client.Roles.Select(role => new Claim("role", role)));
        if (!string.IsNullOrWhiteSpace(client.TenantId))
            claims.Add(new Claim("tenant_id", client.TenantId));

        var key = new RsaSecurityKey(keys.PrivateKey) { KeyId = keys.KeyId };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer: options.Issuer.TrimEnd('/'),
            audience: options.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(options.TokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}