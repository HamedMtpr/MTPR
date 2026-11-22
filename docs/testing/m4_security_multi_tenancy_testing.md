# M4 security and multi-tenancy: testing

- [Back to index](index.md)
- [M4 build and run](../building/m4_security_multi_tenancy_build.md)
- [M4 deployment](../deployment/m4_security_multi_tenancy_deployment.md)
- [M4 operations](../operations/m4_security_multi_tenancy_operations.md)
- [M4 implementation](../coding/m4_security_multi_tenancy_implementation.md)
- [Tenant relation tests](../../tests/Mtpr.IntegrationTests/Security/Tenancy/TenantRelationTests.cs)
- [Tenant scoped relation tests](../../tests/Mtpr.IntegrationTests/Security/Tenancy/TenantScopedRelationsTests.cs)
- [Definition reload tests](../../tests/Mtpr.IntegrationTests/Security/Tenancy/TenantDefinitionReloadTests.cs)
- [Authentication tests](../../tests/Mtpr.IntegrationTests/Security/AuthenticationTests.cs)
- [JWT validation tests](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests.cs)
- [Entity authorization tests](../../tests/Mtpr.IntegrationTests/Security/EntityAuthorizationTests.cs)
- [Field read tests](../../tests/Mtpr.IntegrationTests/Security/FieldReads/FieldReadTests.cs)
- [Field write tests](../../tests/Mtpr.IntegrationTests/Security/FieldWrites/FieldWriteTests.cs)
- [Indirect write tests](../../tests/Mtpr.IntegrationTests/Security/IndirectWrites/IndirectWriteTests.cs)
- [Audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests.cs)
- [Audit upgrade tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests_Legacy.cs)
- [External SQL audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/ExternalSql/ExternalAuditTests.cs)
- [Cascade audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/Cascades/AuditTests_Cascade.cs)
- [JWT audit test](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests_Audit.cs)

## Automated checks

Run from the repository root with the SDK selected by `global.json`:

```powershell
dotnet restore MTPR.slnx
dotnet test MTPR.slnx --no-restore
dotnet test tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj --no-restore --filter 'FullyQualifiedName~Security'
```

JWT tests use simulated OIDC discovery and check valid tokens and invalid issuer, audience, signature and lifetime. Authorization tests exercise role, entity and operation grants and default denial. Field write tests check rejected fields on POST and PATCH, non-object bodies, the PUT `*` requirement, wildcard grants and grants for other roles. Field read tests check removal of ungranted fields from create, get, list and patch responses, rejected `fields`, `sort` and `filter` on hidden fields, the always-readable `id`, wildcard grants and grants for other roles. Audit tests check create, update and delete snapshots, subject and UTC timestamp, and confirm that rule rejection and an audit insertion failure leave the data and audit unchanged. They also check subject and issuer from a validated JWT and upgrade of a legacy audit table. Tests use isolated SQLite databases and do not require an external identity provider.

The tests do not establish interoperability with a particular identity provider or verify tenant isolation against a real identity provider. Cascade audit tests cover cascaded deletes with `caused_by`, set-null updates, restricted deletes leaving no rows, denied requests and the append-only triggers. External SQL tests cover independent committed changes, rollback, cascades, audit failure and conflicting replacement. Reads and 401 responses are outside the change log.

## Manual checks

Use the M4 build guide to run the API and check valid, invalid and insufficiently privileged tokens. For audit inspection, use a disposable database and an appropriate write grant; perform a create, update and delete, then inspect `__mtpr_audit` with a trusted SQLite client. Confirm the operation, record ID, optional subject and issuer, and before/after snapshots. The sample build grants only read access, so it must be extended before testing writes. Snapshots contain data from the test records; do not use sensitive production records.

Row filter tests (`Security/RowFilters`) cover scoped lists, caller filters that cannot widen the scope, missing claims, 404 for out-of-scope access and rollback of an update that would leave the scope.

`RowFilterReloadTests` change configuration and explicitly signal reload while the host is running. They check that a new row filter applies without a restart and that an invalid reload keeps the previous policy. Assigning a configuration value alone does not signal reload.

Tenant isolation tests (`Security/Tenancy/TenantIsolationTests`) cover stamping on create, rejection of creates and updates for another tenant, no cross-tenant list, get, update or delete, a PUT that omits the tenant field, denial without a tenant claim and applying the configuration without a restart.

Tenant relation tests check denial of writes in mixed scoped and unscoped graphs and rejection of an invalid tenant field type. Scoped relation tests check same-tenant references, cross-tenant create and update rejection, rollback of a cross-tenant cascade and a successful same-tenant cascade. The catalog lease test checks that a definition reload waits for an active snapshot. Field read tests check HTTP status before reading the filtered responses, including single-record GET and PATCH.

Indirect write tests verify that defaults and computed changes require field grants, cascade deletes require related entity delete grants, and set-null updates require related field grants. They verify rollback and successful operations with the required role.
