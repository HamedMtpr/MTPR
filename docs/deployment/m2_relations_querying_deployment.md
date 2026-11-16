# M2 relations and querying deployment

- [Back to index](index.md)
- [M2 build guide](../building/m2_relations_querying.md)
- [M2 testing](../testing/m2_relations_querying_testing.md)
- [M2 operations](../operations/m2_relations_querying_operations.md)
- [M1 deployment](m1_vertical_slice_deployment.md)

The M2 API is a prototype without authentication, authorization or tenant isolation. Use a trusted environment. The following example publishes the API and uses a separate directory for the library definitions and a new SQLite database. It requires the .NET 10 SDK and, for a framework-dependent deployment, the .NET 10 ASP.NET Core Runtime on the host.

Run from the repository root in PowerShell:

```powershell
dotnet publish src/Mtpr.Api/Mtpr.Api.csproj -c Release -o ./artifacts/mtpr-api
New-Item -ItemType Directory -Force ./artifacts/m2-data/definitions | Out-Null
Copy-Item docs/examples/m2-library/*.json ./artifacts/m2-data/definitions/
$env:Mtpr__DefinitionsPath = (Resolve-Path ./artifacts/m2-data/definitions).Path
$env:Mtpr__ConnectionString = "Data Source=$((Resolve-Path ./artifacts/m2-data).Path)/mtpr.db"
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
dotnet ./artifacts/mtpr-api/Mtpr.Api.dll
```

Use a fresh data directory for this sample. The process needs read access to all definition files and read/write access to the database directory, including SQLite's companion files. For a separate host, copy the published output and definitions, install the runtime, and configure absolute paths for that host. Keep data and definitions outside the published output so republishing does not replace them.

The API loads definitions and applies migrations on a request. From another PowerShell window, request `http://127.0.0.1:5080/api/author` with `curl.exe`; HTTP 200 and a JSON array confirm that the catalog and database are accessible. Follow the M2 testing guide for relation and query checks.
