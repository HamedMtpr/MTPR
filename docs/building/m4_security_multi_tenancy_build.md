# M4 security and multi-tenancy: build and run

- [Back to index](index.md)
- [M4 implementation](../coding/m4_security_multi_tenancy_implementation.md)
- [M4 deployment](../deployment/m4_security_multi_tenancy_deployment.md)
- [M4 operations](../operations/m4_security_multi_tenancy_operations.md)
- [M4 testing](../testing/m4_security_multi_tenancy_testing.md)
- [Sample order definition](../examples/m3-order/order.json)
- [SDK version](../../global.json)

M4 is in progress. JWT authentication, entity-level role permissions and audit records for direct writes are implemented; field and row authorization, tenant isolation and complete audit coverage are not. Run this prototype only in a trusted environment. Earlier milestone examples predate the current authentication requirement.

## Configure and run

The commands below use PowerShell from the repository root and the SDK selected by `global.json`. They require an existing OIDC identity provider reachable over HTTPS and an access token issued for the API audience with a `reader` role recognized by the JWT bearer handler. Configure the actual issuer and audience; the example values are placeholders and do not provide an identity service.

```powershell
dotnet restore MTPR.slnx
dotnet build MTPR.slnx --no-restore
New-Item -ItemType Directory -Force ./artifacts/m4-demo/definitions | Out-Null
Copy-Item docs/examples/m3-order/order.json ./artifacts/m4-demo/definitions/order.json
$env:Mtpr__DefinitionsPath = (Resolve-Path ./artifacts/m4-demo/definitions).Path
$env:Mtpr__ConnectionString = "Data Source=$((Resolve-Path ./artifacts/m4-demo).Path)/mtpr.db"
$env:Mtpr__Authentication__Authority = 'https://your-issuer.example'
$env:Mtpr__Authentication__Audience = 'your-api-audience'
$env:Mtpr__Authorization__Roles__reader__order__0 = 'read'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
dotnet run --project src/Mtpr.Api --no-launch-profile
```

The service fails to start if authority or audience is missing or the authority is not an absolute HTTPS URL. The sample is bound to loopback HTTP for local use; use TLS for access across a network. Use a fresh data directory when repeating the setup; creating an existing directory does not clear its database.

## Verify access

Keep the API running. In a second PowerShell window, obtain a suitable token through your identity provider's approved flow and place it in the `MTPR_ACCESS_TOKEN` environment variable without committing or sharing it. Then run:

```powershell
$headers = @{ Authorization = "Bearer $env:MTPR_ACCESS_TOKEN" }
Invoke-RestMethod -Uri 'http://127.0.0.1:5080/api/order' -Headers $headers
```

A valid token with the `reader` role receives the order list, initially empty with a fresh database. A missing or invalid token receives HTTP 401. A valid token without a matching role receives HTTP 403. The reader grant does not allow writes. The role name and operation grant must match the actual token and requested endpoint.

To run all automated checks from the repository root:

```powershell
dotnet test MTPR.slnx --no-restore
```

The JWT integration tests simulate the identity provider; they do not require the external provider configured for the manual check.

The `__mtpr_audit` table is created in the SQLite database by the first successful direct create, update or delete. The read-only sample above does not create it. Audit snapshots include record data; use a disposable database when experimenting and protect its contents.