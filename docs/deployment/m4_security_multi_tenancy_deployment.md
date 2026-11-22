# M4 security and multi-tenancy: deployment

- [Back to index](index.md)
- [M4 build and run](../building/m4_security_multi_tenancy_build.md)
- [M4 operations](../operations/m4_security_multi_tenancy_operations.md)
- [M4 implementation](../coding/m4_security_multi_tenancy_implementation.md)
- [M4 testing](../testing/m4_security_multi_tenancy_testing.md)
- [Sample order definition](../examples/m3-order/order.json)
- [SDK version](../../global.json)

This prototype is intended for a trusted environment. Entity grants do not provide field or row isolation; tenant isolation remains unimplemented. Cascaded changes to related entities are audited but not separately authorized. The example uses framework-dependent publish, the pinned SDK to build, and a compatible ASP.NET Core runtime on the target host.

Optional input-field write grants can restrict the properties submitted to POST and PATCH for a configured entity. They do not restrict indirect changes, and PUT requires a wildcard field grant. Configure them alongside entity grants as described in the operations guide and restart the API after changes.

Optional output-field read grants (`Mtpr:Authorization:FieldReads`) hide fields from responses and reject list queries that reference hidden fields. Add them to the same configuration, grant `id` implicitly, and restart the API after changes. Other configured roles' grants never apply to a caller.

## Publish and configure

From the repository root in PowerShell, publish the API and prepare a separate data directory:

```powershell
dotnet publish src/Mtpr.Api/Mtpr.Api.csproj -c Release -o ./artifacts/mtpr-m4-api
New-Item -ItemType Directory -Force ./artifacts/m4-deploy/definitions | Out-Null
Copy-Item docs/examples/m3-order/order.json ./artifacts/m4-deploy/definitions/order.json
$env:Mtpr__DefinitionsPath = (Resolve-Path ./artifacts/m4-deploy/definitions).Path
$env:Mtpr__ConnectionString = "Data Source=$((Resolve-Path ./artifacts/m4-deploy).Path)/mtpr.db"
$env:Mtpr__Authentication__Authority = 'https://your-issuer.example'
$env:Mtpr__Authentication__Audience = 'your-api-audience'
$env:Mtpr__Authorization__Roles__reader__order__0 = 'read'
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
dotnet ./artifacts/mtpr-m4-api/Mtpr.Api.dll
```

Replace issuer and audience with values for your OIDC provider and API. Configure role grants appropriate to the deployment; the sample permits only reading orders. The API must be able to reach the issuer's HTTPS discovery and signing-key endpoints. Bind to a suitable interface and terminate TLS securely for network access. The example listens only on loopback.

The service account needs read access to definitions and read/write access to the database directory and SQLite companion files. Keep definitions and database outside the publish directory, and back up the database and matching definitions together. A different host needs the published files, the matching runtime, definitions and host-specific absolute paths. Protect deployment configuration and the issuer's trust configuration from unauthorized modification.

The first successful direct write creates the audit table in the same SQLite database. It stores full before and after snapshots, which may contain sensitive data. Protect the database and its backups accordingly; the database owner can alter the audit records, and no external or tamper-evident audit sink is provided. A failure to write the audit entry rolls back the associated data change.

Existing audit tables are upgraded with a nullable `subject_issuer` column during the next successful direct write. Back up the database and matching definitions before deployment. Existing rows retain a null issuer; new rows record the validated token's issuer when its claim is present. Ensure the service can alter the SQLite schema as well as write the database. The same upgrade adds `request_id`, `caused_by` and `detail` columns and the append-only triggers; back up first, since the triggers also block manual cleanup.

## Verify

Use the manual checks in the M4 build guide with an appropriate token. Verify both an allowed request and a request with a valid token lacking the grant, as well as an unauthenticated request. A running process alone does not establish that discovery, authorization, definitions and database access work. Use a disposable database when testing writes. Stop the process before replacing its database or restoring backups.

Review `Mtpr:Authorization:RowFilters` and make sure the identity provider issues the claims they reference; a missing claim hides all rows.

Configure a policy for every entity that requires row restrictions. Removing a policy restores unfiltered access for that entity. Test creates and updates as well as reads; out-of-scope resulting records are rejected and rolled back. These filters do not provide complete tenant isolation or restrict cascades and direct database access.

Edit authorization settings in a reloadable source such as `appsettings.json`; they apply without restarting. Settings from environment variables or arguments, and JWT settings, still need a restart. After an edit, check the log for a reload message or an "Invalid ... policy ignored" error.
