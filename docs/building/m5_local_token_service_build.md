# M5 local token service: build and run

- [Back to index](index.md)
- [M5 implementation](../coding/m5_local_token_service_implementation.md)
- [M5 deployment](../deployment/m5_local_token_service_deployment.md)
- [M5 operations](../operations/m5_local_token_service_operations.md)
- [M5 testing](../testing/m5_local_token_service_testing.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)

- [Token service project](../../tools/Mtpr.LocalTokenService/Mtpr.LocalTokenService.csproj)
- [Token service settings](../../tools/Mtpr.LocalTokenService/appsettings.json)
- [SDK version](../../global.json)

`tools/Mtpr.LocalTokenService` is a development tool that issues signed JWT access tokens and publishes the matching OIDC discovery and JWKS documents. It lets a developer run the M4 secured API without an external identity provider. It is not a production identity provider.

## Build

From the repository root, using the SDK selected by `global.json`:

```powershell
dotnet restore MTPR.slnx
dotnet build MTPR.slnx --no-restore
```

## Run

```powershell
dotnet run --project tools/Mtpr.LocalTokenService
```

The default launch profile listens on `https://localhost:59104` and `http://localhost:59105`. Set `LocalTokenService:Issuer` to the externally visible HTTPS base URL (default `https://localhost:5443`); it must equal the issuer the token carries and the authority configured for the API. Trust the ASP.NET Core development certificate first with `dotnet dev-certs https --trust`.

## Obtain a token

```powershell
$body = @{
  grant_type    = 'client_credentials'
  client_id     = 'reader-client'
  client_secret = 'reader-secret-change-me'
}
$response = Invoke-RestMethod -Method Post -Uri 'https://localhost:59104/token' -Body $body
$env:MTPR_ACCESS_TOKEN = $response.access_token
```

The sample clients `reader-client` and `editor-client` are defined in the settings file with the `reader` and `editor` roles. Their secrets are placeholders; replace them in your own configuration.

## Point the API at the service

Start the API as described in the M4 build guide, with the authority and audience set to the token service values:

```powershell
$env:Mtpr__Authentication__Authority = 'https://localhost:59104'
$env:Mtpr__Authentication__Audience = 'mtpr-api'
```

The Authority must match `LocalTokenService:Issuer`. When running on port 59104, set `LocalTokenService__Issuer=https://localhost:59104` before starting the token service.

## Endpoints

| Endpoint | Purpose |
| --- | --- |
| `GET /.well-known/openid-configuration` | OIDC discovery document. |
| `GET /.well-known/jwks.json` | Public signing key (RS256) with its `kid`. |
| `POST /token` | `client_credentials` grant; returns `access_token`, `token_type` and `expires_in`. |
