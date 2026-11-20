# M3 business rules: deployment

- [Deployment index](index.md)
- [M3 build and run](../building/m3_business_rules_build.md)
- [Sample order definition](../examples/m3-order/order.json)
- [M3 operations](../operations/m3_business_rules_operations.md)
- [M3 testing](../testing/m3_business_rules_testing.md)
- [M1 deployment](m1_vertical_slice_deployment.md)
- [SDK version](../../global.json)

This deployment is for a trusted environment: the current API has no authentication, authorization or tenant isolation. Business rules and lifecycle checks are not authorization. The example uses a framework-dependent publish, the pinned .NET SDK to build, and the matching ASP.NET Core runtime on the target host.

From the repository root in PowerShell, publish and prepare a fresh data directory:

```powershell
dotnet publish src/Mtpr.Api/Mtpr.Api.csproj -c Release -o ./artifacts/mtpr-m3-api
New-Item -ItemType Directory -Force ./artifacts/m3-deploy/definitions | Out-Null
Copy-Item docs/examples/m3-order/order.json ./artifacts/m3-deploy/definitions/order.json
$env:Mtpr__DefinitionsPath = (Resolve-Path ./artifacts/m3-deploy/definitions).Path
$env:Mtpr__ConnectionString = "Data Source=$((Resolve-Path ./artifacts/m3-deploy).Path)/mtpr.db"
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
dotnet ./artifacts/mtpr-m3-api/Mtpr.Api.dll
```

The database is created on the first request. The service account needs read access to the definitions and read/write access to the database directory and SQLite companion files. For a different host, copy the publish output and the definition file, install the runtime and set absolute paths for that host. Keep the database and definitions outside the publish output and back up the matching set before changes.

In a second PowerShell window, verify both catalog loading and business behavior:

```powershell
$base = 'http://127.0.0.1:5080/api/order'
Invoke-RestMethod -Method Get -Uri $base
$order = Invoke-RestMethod -Method Post -Uri $base -ContentType 'application/json' -Body (@{ title = 'Pens'; price = 10 } | ConvertTo-Json)
$order | Format-List
```

An empty array is expected for the first GET on a fresh database. The POST returns an id, initial status `draft`, quantity `1` and total `10`. Continue with the build guide to check transition and rejection behavior. These checks create a real record; use a disposable sample database.

On shutdown, stop the API process. Publish output and the data directory have separate lifecycles; republishing must not overwrite the database or its definitions. The API checks definitions on requests, not during startup, so a running process alone does not prove that its definitions and database are usable.
