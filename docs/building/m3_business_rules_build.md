# M3 business rules: build and run

- [Building index](index.md)
- [Sample order definition](../examples/m3-order/order.json)
- [M3 deployment](../deployment/m3_business_rules_deployment.md)
- [M3 operations](../operations/m3_business_rules_operations.md)
- [M3 testing](../testing/m3_business_rules_testing.md)
- [M3 implementation and supported expression syntax](../coding/m3_business_rules_implementation.md)
- [SDK version](../../global.json)

The sample demonstrates a default, a computed decimal field, a rejection rule and an entity lifecycle. It is an API prototype without authentication, authorization or tenant isolation; run it only in a trusted environment. The commands below use PowerShell from the repository root and require the SDK selected by `global.json`.

Create a fresh sample data directory and run the API in the first PowerShell window:

```powershell
dotnet restore MTPR.slnx
dotnet build MTPR.slnx --no-restore
New-Item -ItemType Directory -Force ./artifacts/m3-demo/definitions | Out-Null
Copy-Item docs/examples/m3-order/order.json ./artifacts/m3-demo/definitions/order.json
$env:Mtpr__DefinitionsPath = (Resolve-Path ./artifacts/m3-demo/definitions).Path
$env:Mtpr__ConnectionString = "Data Source=$((Resolve-Path ./artifacts/m3-demo).Path)/mtpr.db"
$env:ASPNETCORE_URLS = 'http://127.0.0.1:5080'
dotnet run --project src/Mtpr.Api --no-launch-profile
```

Keep the API running. In a second PowerShell window, create an order and inspect its default and computed fields:

```powershell
$base = 'http://127.0.0.1:5080/api/order'
$order = Invoke-RestMethod -Method Post -Uri $base -ContentType 'application/json' -Body (@{ title = 'Pens'; price = 10 } | ConvertTo-Json)
$order | Format-List
$id = $order.id
```

The response has `quantity: 1`, `status: "draft"`, and `total: 10`. `total` is read-only; including it in a POST, PUT or PATCH fails validation. The definition is loaded and its table is created on the first request.

Apply an allowed transition and change the quantity:

```powershell
Invoke-RestMethod -Method Patch -Uri "$base/$id" -ContentType 'application/json' -Body (@{ status = 'submitted'; quantity = 2 } | ConvertTo-Json)
```

The response has `status: "submitted"` and `total: 20`. A PATCH with `quantity: 6` returns HTTP 422 and `MTPR-2011` because the before-update rule rejects the computed total of 60; the record remains unchanged. A PATCH from `submitted` to `draft` returns HTTP 422 and `MTPR-2012` because that transition is not configured. PowerShell's `Invoke-RestMethod` raises an error for these responses. An example that reports the rejected rule response is:

```powershell
try {
    Invoke-RestMethod -Method Patch -Uri "$base/$id" -ContentType 'application/json' -Body (@{ quantity = 6 } | ConvertTo-Json)
} catch {
    $_.ErrorDetails.Message
}
Invoke-RestMethod -Method Get -Uri "$base/$id"
```

To exercise all available automated checks, run this from the repository root:

```powershell
dotnet test MTPR.slnx
```

A file change is picked up on a subsequent request. Existing schema changes remain restricted to the documented additive changes. Changing a computed expression does not recalculate existing rows until each row is successfully updated. Use a fresh data directory when repeating the setup; `New-Item -Force` does not clear an existing database.
