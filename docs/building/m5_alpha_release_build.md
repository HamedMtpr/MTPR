# m5_alpha_release: build and run

- [Back to index](index.md)
- [M5 implementation](../coding/m5_alpha_release_implementation.md)
- [M5 deployment](../deployment/m5_alpha_release_deployment.md)
- [M5 operations](../operations/m5_alpha_release_operations.md)
- [M5 testing](../testing/m5_alpha_release_testing.md)
- [M4 security and multi-tenancy build](m4_security_multi_tenancy_build.md)
- [Token service project](../../tools/Mtpr.LocalTokenService/Mtpr.LocalTokenService.csproj)
- [Token service settings](../../tools/Mtpr.LocalTokenService/appsettings.json)
- [Helpdesk sample definitions](../examples/m5-helpdesk/README.md)
- [Sample integration tests](../../tests/Mtpr.IntegrationTests/HelpdeskSample/HelpdeskSampleTests.cs)
- [Dockerfile](../../Dockerfile)
- [SDK version](../../global.json)

M5 adds two parts that are built and run together:

- `tools/Mtpr.LocalTokenService` is a development tool that issues signed JWT access tokens and publishes the matching OIDC discovery and JWKS documents. It lets a developer run the M4 secured API without an external identity provider. It is not a production identity provider.
- The helpdesk sample application (customers, tickets, ticket comments) is defined only by the JSON files in `docs/examples/m5-helpdesk`. No code is written for it.

## Build

From the repository root, using the SDK selected by `global.json`:

```powershell
dotnet restore MTPR.slnx
dotnet build MTPR.slnx --no-restore
```

## Run the token service

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:LocalTokenService__Issuer = 'https://localhost:5443'
dotnet run --project tools/Mtpr.LocalTokenService --no-launch-profile --urls https://localhost:5443
```

The token service listens on `https://localhost:5443`. `LocalTokenService:Issuer` must be the externally visible HTTPS base URL of the service: it is the issuer the token carries, the base of the published JWKS address and the authority configured for the API. Trust the ASP.NET Core development certificate first with `dotnet dev-certs https --trust`.

## Obtain a token

```powershell
$body = @{
  grant_type    = 'client_credentials'
  client_id     = 'reader-client'
  client_secret = 'reader-secret-change-me'
}
$response = Invoke-RestMethod -Method Post -Uri 'https://localhost:5443/token' -Body $body
$env:MTPR_ACCESS_TOKEN = $response.access_token
```

The sample clients `reader-client` and `editor-client` are defined in the settings file with the `reader` and `editor` roles. Their secrets are placeholders; replace them in your own configuration.

## Token service endpoints

| Endpoint | Purpose |
| --- | --- |
| `GET /.well-known/openid-configuration` | OIDC discovery document. |
| `GET /.well-known/jwks.json` | Public signing key (RS256) with its `kid`. |
| `POST /token` | `client_credentials` grant; returns `access_token`, `token_type` and `expires_in`. |

## Run the sample application

Keep the token service running and start the API in a second window. The authority and audience are the token service issuer and audience. The definitions folder is watched, so you can edit the JSON files while the API runs and the next request uses the change without a restart.

```powershell
$env:Mtpr__DefinitionsPath = "$PWD/docs/examples/m5-helpdesk"
$env:Mtpr__ConnectionString = "Data Source=$PWD/helpdesk.db"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Mtpr__Authentication__Authority = 'https://localhost:5443'
$env:Mtpr__Authentication__Audience = 'mtpr-api'
$env:Mtpr__Authorization__Roles__editor__customer__0 = "read"
$env:Mtpr__Authorization__Roles__editor__customer__1 = "create"
$env:Mtpr__Authorization__Roles__editor__ticket__0 = "read"
$env:Mtpr__Authorization__Roles__editor__ticket__1 = "create"
$env:Mtpr__Authorization__Roles__editor__ticket__2 = "update"
$env:Mtpr__Authorization__Roles__editor__ticketcomment__0 = "create"
dotnet run --project src/Mtpr.Api --no-launch-profile --urls http://127.0.0.1:5080
```

The API listens on `http://127.0.0.1:5080`.

## Try the sample

```powershell
$api = 'http://127.0.0.1:5080'

$token = (Invoke-RestMethod -Method Post -Uri 'https://localhost:5443/token' -Body @{
    grant_type    = 'client_credentials'
    client_id     = 'editor-client'
    client_secret = 'editor-secret-change-me'
}).access_token

$headers = @{ Authorization = "Bearer $token" }

$customer = Invoke-RestMethod -Method Post -Uri "$api/api/customer" -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{ name = 'Ada'; email = 'ada@example.com' } | ConvertTo-Json)

$ticket = Invoke-RestMethod -Method Post -Uri "$api/api/ticket" -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{ customerId = $customer.id; subject = 'Cannot log in' } | ConvertTo-Json)

$ticket | Select-Object status, priority, cost
```

The new ticket has status `open`, priority `2` and cost `40` from the definition defaults and the computed field. Moving it to `closed` directly is rejected; `inProgress` is accepted. A ticket with more than 40 hours is rejected by the rule.

## Container

```powershell
docker build -t mtpr-helpdesk .
```

## Automated check

```powershell
dotnet test tests/Mtpr.IntegrationTests --filter "FullyQualifiedName~HelpdeskSample"
```
