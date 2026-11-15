# M1 vertical slice deployment

- [Back to index](index.md)

This guide publishes the M1 API for a single host. M1 is a prototype: it has no authentication, authorization, or tenant isolation. Run it in a trusted environment; it is not ready to be exposed publicly.

## Prerequisites

- .NET 10 SDK on the build machine and .NET 10 ASP.NET Core Runtime on the host.
- A writable directory for the SQLite database and permission to read the definitions directory. SQLite may also need to create files alongside the database.
- Network access to the configured listening address from the client used for verification.

## Publish and start

The following PowerShell example builds a framework-dependent deployment on the same machine. Run it from the repository root. It keeps definitions and data outside the published binaries so they survive a republish.

```powershell
dotnet publish src/Mtpr.Api/Mtpr.Api.csproj -c Release -o ./artifacts/mtpr-api
New-Item -ItemType Directory -Force ./artifacts/mtpr-data/definitions | Out-Null
Copy-Item src/Mtpr.Api/definitions/product.json ./artifacts/mtpr-data/definitions/
$env:Mtpr__DefinitionsPath = (Resolve-Path ./artifacts/mtpr-data/definitions).Path
$env:Mtpr__ConnectionString = "Data Source=$((Resolve-Path ./artifacts/mtpr-data).Path)/mtpr.db"
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
dotnet ./artifacts/mtpr-api/Mtpr.Api.dll
```

For a separate host, copy the published directory and the desired definition files to that host, install the runtime, and set the same environment variables to paths on that host before starting the DLL. Do not overwrite an existing definition file with a changed version: M1 rejects changes to definitions already applied to the database. Use a service manager appropriate to the host if the process should be started automatically.

`Mtpr__DefinitionsPath` selects the directory containing one entity per JSON file; relative paths are resolved against the application content root. `Mtpr__ConnectionString` is a Microsoft.Data.Sqlite connection string. Prefer an absolute database path because the default `Data Source=mtpr.db` is relative to the process working directory. The API reads definitions and creates tables on the first request, not at startup. See the [build guide](../building/m1_vertical_slice_build.md) for the definition format.

## Verify

From another PowerShell window on the host:

```powershell
curl.exe -i http://127.0.0.1:5080/api/product
```

An HTTP 200 response with a JSON array confirms that the API loaded the sample definition and can access the database. See [M1 testing](../testing/m1_vertical_slice_testing.md) for CRUD and validation checks, and [M1 operations](../operations/m1_vertical_slice_operations.md) for configuration and recovery.
