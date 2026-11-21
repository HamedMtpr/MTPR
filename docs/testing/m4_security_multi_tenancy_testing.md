# M4 security and multi-tenancy: testing

- [Back to index](index.md)
- [M4 build and run](../building/m4_security_multi_tenancy_build.md)
- [M4 deployment](../deployment/m4_security_multi_tenancy_deployment.md)
- [M4 operations](../operations/m4_security_multi_tenancy_operations.md)
- [M4 implementation](../coding/m4_security_multi_tenancy_implementation.md)
- [Authentication tests](../../tests/Mtpr.IntegrationTests/Security/AuthenticationTests.cs)
- [JWT validation tests](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests.cs)
- [Entity authorization tests](../../tests/Mtpr.IntegrationTests/Security/EntityAuthorizationTests.cs)
- [Audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests.cs)
- [Audit upgrade tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests_Legacy.cs)
- [JWT audit test](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests_Audit.cs)

## Automated checks

Run from the repository root with the SDK selected by `global.json`:

```powershell
dotnet restore MTPR.slnx
dotnet test MTPR.slnx --no-restore
dotnet test tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~Security'
```

JWT tests use simulated OIDC discovery and check valid tokens and invalid issuer, audience, signature and lifetime. Authorization tests exercise role, entity and operation grants and default denial. Audit tests check create, update and delete snapshots, subject and UTC timestamp, and confirm that rule rejection and an audit insertion failure leave the data and audit unchanged. They also check subject and issuer from a validated JWT, upgrade of a legacy audit table, and rollback of the upgrade when insertion fails. Tests use isolated SQLite databases and do not require an external identity provider.

The tests do not establish interoperability with a particular identity provider or verify tenant isolation. Cascades, direct database changes and failed or denied requests are outside the current audit coverage.

## Manual checks

Use the M4 build guide to run the API and check valid, invalid and insufficiently privileged tokens. For audit inspection, use a disposable database and an appropriate write grant; perform a create, update and delete, then inspect `__mtpr_audit` with a trusted SQLite client. Confirm the operation, record ID, optional subject and issuer, and before/after snapshots. The sample build grants only read access, so it must be extended before testing writes. Snapshots contain data from the test records; do not use sensitive production records.
