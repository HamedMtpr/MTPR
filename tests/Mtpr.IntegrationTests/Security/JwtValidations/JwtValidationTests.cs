using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.JwtValidations;

public partial class JwtValidationTests : IntegrationTest
{
    private const string issuer = "https://identity.example.invalid";
    private const string audience = "mtpr-tests";

    [Theory]
    [InlineData("valid", HttpStatusCode.OK)]
    [InlineData("issuer", HttpStatusCode.Unauthorized)]
    [InlineData("audience", HttpStatusCode.Unauthorized)]
    [InlineData("signature", HttpStatusCode.Unauthorized)]
    [InlineData("expired", HttpStatusCode.Unauthorized)]
    [InlineData("not-yet-valid", HttpStatusCode.Unauthorized)]
    public async Task Bearer_tokens_are_validated_against_oidc_metadata(string scenario,
        HttpStatusCode expected)
    {
        using var issuerKey = RSA.Create(2048);
        using var otherKey = RSA.Create(2048);
        var publicKey = issuerKey.ExportParameters(false);
        var jsonWebKeySet = JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = "test-key",
                    alg = "RS256",
                    n = Base64UrlEncoder.Encode(publicKey.Modulus), 
                    e = Base64UrlEncoder.Encode(publicKey.Exponent)
                }
            }
        });

        using var backchannel = new HttpClient(
            new JwtValidationTests.TestDiscoveryHandler(jsonWebKeySet));

        await using MtprApiFactory factory = new();
        factory.UseTestAuthentication = false;
        factory.ConfigureJwtBearer = options => options.Backchannel = backchannel;
        factory.Define("product", 
            """{"name":"product","fields":[{"name":"title","type":"string"}]}""");

        using var client = factory.CreateClient();

        var now = DateTime.UtcNow; 
        
        RsaSecurityKey signingKey = new (
            scenario == "signature" 
                ? otherKey 
                : issuerKey)
        {
            KeyId = "test-key"
        };

        var token = new JwtSecurityToken(
            issuer: scenario == "issuer" 
                ? "https://other.example.invalid" 
                : issuer,
            audience: scenario == "audience" ? "other-api" : audience,
            claims: [new Claim("sub", "test-user")],
            notBefore: scenario == "not-yet-valid" 
                ? now.AddMinutes(10) 
                : now.AddMinutes(-20),
            expires: scenario == "expired" 
                ? now.AddMinutes(-10) 
                : now.AddMinutes(20),
            signingCredentials: 
            new SigningCredentials(signingKey,
                SecurityAlgorithms.RsaSha256));

        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", 
                new JwtSecurityTokenHandler()
                    .WriteToken(token));

        using var response = await client.GetAsync("/api/product", 
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);
    }
}
