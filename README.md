# Project Summary

MTPR is a metadata-driven backend generator designed to turn configuration and business rules into backend systems for a range of application domains. The current prototype supports file-defined entities, SQLite-backed CRUD, relations and querying, and configurable defaults, rules, computed decimal fields and lifecycle transitions. The API supports JWT authentication, entity and field role grants, row filters, tenant isolation and transactional audit records for managed SQLite tables. These protections have documented configuration and deployment boundaries; use the prototype only in a trusted environment.

- [Project roadmap](docs/planning/project_roadmap.md)
## Getting started

## Quickstart

Install the .NET SDK version listed in `global.json` and Git. Run these commands in PowerShell. Use three windows: the token service runs in the first, the API in the second, and the sample requests in the third.

```powershell
git clone https://github.com/HamedMtpr/MTPR.git
cd MTPR
dotnet dev-certs https --trust
dotnet restore MTPR.slnx
dotnet build MTPR.slnx --no-restore
```

Window 1, token service:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:LocalTokenService__Issuer = 'https://localhost:5443'
dotnet run --project tools/Mtpr.LocalTokenService --no-launch-profile --urls https://localhost:5443
```

Window 2, API with the helpdesk sample (run from the repository root):

```powershell
$env:Mtpr__DefinitionsPath = "$PWD/docs/examples/m5-helpdesk"
$env:Mtpr__ConnectionString = "Data Source=$PWD/helpdesk.db"
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:Mtpr__Authentication__Authority = 'https://localhost:5443'
$env:Mtpr__Authentication__Audience = 'mtpr-api'
$env:Mtpr__Authorization__Roles__editor__customer__0 = 'read'
$env:Mtpr__Authorization__Roles__editor__customer__1 = 'create'
$env:Mtpr__Authorization__Roles__editor__ticket__0 = 'read'
$env:Mtpr__Authorization__Roles__editor__ticket__1 = 'create'
dotnet run --project src/Mtpr.Api --no-launch-profile --urls http://127.0.0.1:5080
```

Window 3, create a customer and a ticket:

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
Invoke-RestMethod -Method Post -Uri "$api/api/ticket" -Headers $headers `
    -ContentType 'application/json' `
    -Body (@{ customerId = $customer.id; subject = 'Cannot log in' } | ConvertTo-Json)
```

The ticket is returned with status `open`, priority `2` and cost `40`. Edit a JSON file in `docs/examples/m5-helpdesk` while the API runs and the next request uses the change without a restart. The client secrets are placeholders for local use only.

Guides per milestone:

- [M0 foundation hardening](docs/planning/m0_foundation_hardening.md)
- [M1 vertical slice build](docs/building/m1_vertical_slice_build.md)
- [M2 relations and querying](docs/building/m2_relations_querying.md)
- [M3 business rules: build and run](docs/building/m3_business_rules_build.md)
- [M4 security and multi-tenancy: build and run](docs/building/m4_security_multi_tenancy_build.md)
- [M5 alpha release: build and run](docs/building/m5_alpha_release_build.md)

## Test coverage

The latest test coverage report shows 93% statement coverage (1,665 of 1,784) for the projects under `src`: `Mtpr.Core` 100% (77/77), `Mtpr.Api` 96% (471/492), and `Mtpr.Engine` 92% (1,117/1,215). The default no-op audit, tenant, and write-authorization types were not exercised. `tools/Mtpr.LocalTokenService` is not included in the report and has no automated tests.

## Documentation

- [Planning](docs/planning/index.md)
- [Code contribution](docs/coding/index.md)
- [Building](docs/building/index.md)
- [Releasing](docs/releasing/index.md)
- [Testing](docs/testing/index.md)
- [Deployment](docs/deployment/index.md)
- [Operations](docs/operations/index.md)
- [Monitoring](docs/monitoring/index.md)
