using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.IdentityModel.Tokens;

namespace Mtpr.IntegrationTests.Security.JwtValidations;

public partial class JwtValidationTests
{
    [Fact]
    public async Task Validated_token_issuer_and_subject_are_recorded_in_audit()
    {
        using var rsa = RSA.Create(2048);
        var key = rsa.ExportParameters(false);
        var keys = JsonSerializer.Serialize(new
        {
            keys = new[]
            {
                new
                {
                    kty = "RSA",
                    use = "sig",
                    kid = "test-key",
                    alg = "RS256",
                    n = Base64UrlEncoder.Encode(key.Modulus),
                    e = Base64UrlEncoder.Encode(key.Exponent)
                }
            }
        });

        using HttpClient backchannel = new(new TestDiscoveryHandler(keys));
        await using MtprApiFactory factory = new();
        factory.UseTestAuthentication = false;
        factory.ConfigureJwtBearer = options => options.Backchannel = backchannel;
        factory.Define("product", """
            {
              "name": "product",
              "fields": [
                {
                  "name": "title",
                  "type": "string"
                }
              ]
            }
            """);

        using var client = factory.CreateClient();
        RsaSecurityKey signingKey = new(rsa) { KeyId = "test-key" };
        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims:
            [
                new Claim("sub", "test-user"),
                new Claim(ClaimTypes.Role, "test-role")
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(signingKey,
                SecurityAlgorithms.RsaSha256));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer",
                new JwtSecurityTokenHandler().WriteToken(token));

        var ct = TestContext.Current.CancellationToken;
        using var response = await client.PostAsJsonAsync("/api/product",
            new { title = "test" },
            ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT subject, subject_issuer FROM __mtpr_audit";
        await using var reader = await command.ExecuteReaderAsync(ct);

        (await reader.ReadAsync(ct)).Should().BeTrue();
        reader.GetString(0).Should().Be("test-user");
        reader.GetString(1).Should().Be(issuer);
        (await reader.ReadAsync(ct)).Should().BeFalse();
    }
}
