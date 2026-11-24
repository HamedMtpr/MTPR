using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Mtpr.LocalTokenService;
using Mtpr.LocalTokenService.SigningKeys;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<LocalTokenOptions>()
    .Bind(builder.Configuration
        .GetSection(LocalTokenOptions.SectionName));

// Order matters: configuration (User Secrets / env var) wins over the file.
builder.Services.AddSingleton<ISigningKeySource, ConfigurationSigningKeySource>();
builder.Services.AddSingleton<ISigningKeySource, FileSigningKeySource>();
builder.Services.AddSingleton<ISigningKeyProvider, ChainedSigningKeyProvider>();
builder.Services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

var app = builder.Build();

app.MapGet("/.well-known/openid-configuration", 
    (IOptionsMonitor<LocalTokenOptions> options) =>
{
    var issuer = options.CurrentValue.Issuer.TrimEnd('/');
    return Results.Ok(new
    {
        issuer,
        jwks_uri = $"{issuer}/.well-known/jwks.json",
        token_endpoint = $"{issuer}/token",
        id_token_signing_alg_values_supported = new[] { "RS256" },
        grant_types_supported = new[] { "client_credentials" }
    });
});

app.MapGet("/.well-known/jwks.json", 
    (ISigningKeyProvider keys) =>
{
    var parameters = keys.PublicKey.ExportParameters(false);

    var jwk = JsonWebKeyConverter.ConvertFromRSASecurityKey(
        new RsaSecurityKey(parameters));
    jwk.Kid = keys.KeyId;
    jwk.Use = "sig";
    jwk.Alg = SecurityAlgorithms.RsaSha256;

    return Results.Ok(new { keys = new[] { jwk } });
});

app.MapPost("/token", 
    (HttpRequest request, 
        IOptionsMonitor<LocalTokenOptions> options,
        ITokenIssuer issuer) =>
{
    static IResult oAuthError(string error,
        int status = StatusCodes.Status400BadRequest)
        => Results.Json(
            new { error },
            statusCode: status);

    var form = request.Form;

    if (form["grant_type"] != "client_credentials")
        return oAuthError("unsupported_grant_type");

    var config = options.CurrentValue;
    var clientId = form["client_id"].ToString();
    if (!config.Clients.TryGetValue(clientId, out var client)
        || !fixedTimeEquals(client.Secret, form["client_secret"].ToString()))
        return oAuthError("invalid_client", StatusCodes.Status401Unauthorized);

    var token = issuer.Issue(client, config);

    return Results.Ok(new
    {
        access_token = token,
        token_type = "Bearer",
        expires_in = config.TokenLifetimeMinutes * 60
    });
});

app.Run();
return;

static bool fixedTimeEquals(string expected, string actual)
    => CryptographicOperations.FixedTimeEquals(
        System.Text.Encoding.UTF8.GetBytes(expected),
        System.Text.Encoding.UTF8.GetBytes(actual));