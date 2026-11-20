# M3 business rules: testing

- [Back to index](index.md)
- [M3 build and run](../building/m3_business_rules_build.md)
- [M3 deployment](../deployment/m3_business_rules_deployment.md)
- [M3 operations](../operations/m3_business_rules_operations.md)
- [Sample order definition](../examples/m3-order/order.json)
- [M3 implementation and expression syntax](../coding/m3_business_rules_implementation.md)
- [Integration test project](../../tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj)
- [Default value tests](../../tests/Mtpr.IntegrationTests/FieldDefaultTests.cs)
- [Create rule tests](../../tests/Mtpr.IntegrationTests/CreateRules/CreateRuleTests.cs)
- [Transactional rule tests](../../tests/Mtpr.IntegrationTests/TransactionalRules/TransactionalRuleTests.cs)
- [Lifecycle tests](../../tests/Mtpr.IntegrationTests/Lifecycles/LifecycleTests.cs)
- [Computed value tests](../../tests/Mtpr.IntegrationTests/ComputedValues/ComputedValueTests.cs)
- [Definition schema tests](../../tests/Mtpr.Tests/M0FoundationHardeningSchema/M0FoundationHardeningSchemaTests.cs)

## Automated tests

Run from the repository root with the SDK selected by `global.json`:

```powershell
dotnet restore MTPR.slnx
dotnet test MTPR.slnx --no-restore
```

To run the M3 integration test classes and definition schema tests separately:

```powershell
dotnet test tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~FieldDefaultTests|FullyQualifiedName~CreateRuleTests|FullyQualifiedName~TransactionalRuleTests|FullyQualifiedName~LifecycleTests|FullyQualifiedName~ComputedValueTests'
dotnet test tests/Mtpr.Tests/Mtpr.Tests.csproj --no-restore --filter 'FullyQualifiedName~M0FoundationHardeningSchemaTests'
```

The integration tests exercise the API against isolated SQLite databases. They cover:

- Defaults for omitted create fields, explicit values and changed definitions.
- Rule condition validation and rejection at all six create, update and delete triggers, including transaction rollback and a cascade rollback.
- Initial lifecycle state, allowed and rejected transitions, concurrent transitions, configuration changes and rollback after a rejecting rule.
- Computed values on create and update, read-only fields, invalid expressions, overflow, null operands, concurrent updates and expression changes.
- Concurrent updates that would otherwise violate a combined rule.

The schema tests verify definition shape. Runtime validation and transactional behavior are exercised separately by integration tests. The sample below uses a `beforeUpdate` rule; the automated tests cover the other triggers.

## Manual API checks

Start the sample API with a fresh, disposable database using the M3 build guide above. Keep it running and execute these commands in a second PowerShell window from the repository root. These requests create and update a real record.

```powershell
$base = 'http://127.0.0.1:5080/api/order'
$order = Invoke-RestMethod -Method Post -Uri $base -ContentType 'application/json' -Body (@{ title = 'M3 test'; price = 10 } | ConvertTo-Json)
$order | Format-List
$id = $order.id
Invoke-RestMethod -Method Patch -Uri "$base/$id" -ContentType 'application/json' -Body (@{ status = 'submitted'; quantity = 2 } | ConvertTo-Json)
```

The created order has `quantity: 1`, `status: "draft"` and `total: 10`. The PATCH succeeds with `status: "submitted"`, `quantity: 2` and `total: 20`.

The following requests must fail. PowerShell raises an exception for each HTTP 422 response; the response body identifies the error. Check the HTTP status and error code for each case:

```powershell
# The computed total of 60 violates the before-update rule: MTPR-2011.
try {
    Invoke-RestMethod -Method Patch -Uri "$base/$id" -ContentType 'application/json' -Body (@{ quantity = 6 } | ConvertTo-Json)
} catch {
    "HTTP $([int]$_.Exception.Response.StatusCode)"
    $_.ErrorDetails.Message
}

# The submitted-to-draft transition is not allowed: MTPR-2012.
try {
    Invoke-RestMethod -Method Patch -Uri "$base/$id" -ContentType 'application/json' -Body (@{ status = 'draft' } | ConvertTo-Json)
} catch {
    "HTTP $([int]$_.Exception.Response.StatusCode)"
    $_.ErrorDetails.Message
}

# A client cannot write a computed field: MTPR-2000 with MTPR-2013.
try {
    Invoke-RestMethod -Method Patch -Uri "$base/$id" -ContentType 'application/json' -Body (@{ total = 1 } | ConvertTo-Json)
} catch {
    "HTTP $([int]$_.Exception.Response.StatusCode)"
    $_.ErrorDetails.Message
}

Invoke-RestMethod -Method Get -Uri "$base/$id" | Format-List
```

The final GET must still show `status: "submitted"`, `quantity: 2` and `total: 20`: none of the rejected updates persisted. Stop the API after testing. To repeat from a clean state, use a new data directory; creating an existing directory does not clear its database.
