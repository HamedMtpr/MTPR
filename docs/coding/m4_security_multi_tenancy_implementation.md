# M4 security and multi-tenancy: implementation

- [Back to index](index.md)
- [M4 build and run](../building/m4_security_multi_tenancy_build.md)
- [M4 deployment](../deployment/m4_security_multi_tenancy_deployment.md)
- [M4 operations](../operations/m4_security_multi_tenancy_operations.md)
- [M4 testing](../testing/m4_security_multi_tenancy_testing.md)
- [API entry point](../../src/Mtpr.Api/Program.cs)
- [Entity permissions](../../src/Mtpr.Api/EntityPermissions.cs)
- [JWT validation tests](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests.cs)
- [Entity authorization tests](../../tests/Mtpr.IntegrationTests/Security/EntityAuthorizationTests.cs)
- [Field write permissions](../../src/Mtpr.Api/Authorization/FieldWritePermissions.cs)
- [Field grants](../../src/Mtpr.Api/Authorization/FieldGrants.cs)
- [Field read permissions](../../src/Mtpr.Api/Authorization/FieldReadPermissions.cs)
- [Field read tests](../../tests/Mtpr.IntegrationTests/Security/FieldReads/FieldReadTests.cs)
- [Field write tests](../../tests/Mtpr.IntegrationTests/Security/FieldWrites/FieldWriteTests.cs)
- [Audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests.cs)
- [Audit upgrade tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests_Legacy.cs)
- [JWT audit test](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests_Audit.cs)
- [SQLite record store](../../src/Mtpr.Engine/Persistence/SqliteRecordStore.cs)
- [Cascade planning](../../src/Mtpr.Engine/Persistence/SqliteRecordStore.Cascade.cs)
- [Audit table](../../src/Mtpr.Engine/Persistence/Audit/AuditTable.cs)
- [Audit entry](../../src/Mtpr.Engine/Persistence/Audit/AuditEntry.cs)
- [Audit trail for denials](../../src/Mtpr.Engine/Persistence/Audit/AuditTrail.cs)
- [Audit actor accessor](../../src/Mtpr.Api/HttpAuditActorAccessor.cs)
- [Cascade audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests_Cascade.cs)

## Authentication

The API registers ASP.NET Core JWT bearer authentication and requires an authenticated principal on the `/api/{entity}` group. Startup requires an absolute HTTPS `Mtpr:Authentication:Authority` and a nonempty `Mtpr:Authentication:Audience`. The bearer handler uses the authority's OIDC discovery metadata and signing keys; it validates the token signature, issuer, audience and lifetime. Discovery requires HTTPS. Requests without a valid bearer token receive HTTP 401.

## Entity authorization

`EntityPermissions` reads `Mtpr:Authorization:Roles` once at startup. Each role contains entity names mapped to arrays of operations. Roles are evaluated with `ClaimsPrincipal.IsInRole`; the identity provider must issue a role claim that the bearer handler maps to the principal's role claim type. Role values use the principal's case-sensitive role matching. Entity names are compared case-insensitively, and `*` matches every entity. Operations are case-sensitive and must be `read`, `create`, `update`, or `delete`; an invalid operation in an entity's array prevents startup.

The endpoint filter maps list and single-record GET to `read`, POST to `create`, PUT and PATCH to `update`, and DELETE to `delete`. Any matching grant from any role allows the operation. Without a matching grant the filter returns HTTP 403 before the entity service runs. Permissions do not implicitly grant other operations. Restart the API after changing grants; definition files follow their separate reload behavior.

These checks protect the HTTP endpoint group; direct calls to the engine or database do not pass through the filter. Database cascades and set-null actions on related records do not check the affected entity's grants. Create and update responses can include fields even when the caller does not have the entity's `read` grant.

## Field write grants

`Mtpr:Authorization:FieldWrites` optionally lists allowed input fields by entity and role. A configured entity denies writes from roles without a field grant. Matching role grants are combined, field and entity names are case-insensitive, and `*` in a role's field list grants all input fields for that entity. An entity with no field policy retains the existing entity-level behavior. The policy is loaded at startup and changes require restart. A wildcard entity is not supported.

`Mtpr:Authorization:FieldReads` uses the same shape (`FieldGrants` parses both) to list readable output fields by entity and role. For a configured entity the endpoint filter in `Program.cs` first rejects with HTTP 403 any list request whose `fields`, `sort` or `filter` names a field that is not granted, because ordering or filtering by a hidden field would reveal its values. `id` is always readable. After the handler runs, `FieldReadPermissions.Filter` projects the record or records of GET, POST, PUT and PATCH responses to granted fields. A restricted entity returning an unexpected value result throws instead of leaking it. Roles without a grant on a configured entity cannot read it. Entities without a policy are unchanged.

The endpoint filter first requires the entity operation grant. On POST and PATCH, every property in an object body must be in the caller's field grants; otherwise the request returns HTTP 403. On PUT the caller must have the `*` field grant, because omitted fields can also be modified. A body that is not a JSON object (for example an array or a missing body) is rejected with HTTP 403 on configured entities. The normal engine validation continues to reject unknown, duplicate and computed input fields.

These grants apply only to properties supplied directly through the API, not to the stored values of fields in general. Defaults, lifecycle initial values, computed values, and SQLite cascades can still modify fields that a caller cannot supply. Direct engine or database access bypasses the endpoint filter. The policy does not restrict output, filtering or sorting by a field. It is not field confidentiality or complete field-level authorization.

## Transactional audit records

The SQLite record store creates `__mtpr_audit` on the first successful direct write. Create, update and delete append one entry after their rules succeed and before the same transaction commits. If audit insertion fails, the data write rolls back. Each row contains an incrementing sequence, UTC timestamp, entity, record ID, operation, optional subject and subject issuer, and JSON snapshots before and after the operation. A successful update is recorded even if its values did not change. Reads and failed or rejected writes do not append an entry.

For API requests, `HttpAuditActorAccessor` obtains the principal's name-identifier and `iss` claims; missing claims produce null values. The normal bearer handler validates the token issuer before a write reaches the engine. Other engine hosts can register an `IAuditActorAccessor`; the default reports null values, and custom accessors are responsible for their own identity provenance. The optional issuer distinguishes equal subjects from different issuers, but neither field is guaranteed to exist. Snapshots contain all record fields and may include sensitive values. The audit table is in the same database and is neither tamper-proof nor independently retained.

An existing audit table without `subject_issuer` receives the nullable column during the next successful direct write, inside the write transaction. Existing rows retain their values and have a null issuer; it is not possible to infer their historical issuer. A failed write rolls the schema change back as well. The default interface implementation returns null for issuer so existing custom accessors can continue to operate.

### Cascades, denials and integrity

`SqliteSchemaMigrator` emits `ON DELETE CASCADE`, `SET NULL` or `RESTRICT` clauses and the connection enables `foreign_keys`, so SQLite itself performs these actions. The store leaves that to the database and only works out beforehand what it will touch. Before the root `DELETE`, `planCascadeAsync` walks the referencing fields breadth-first inside the delete transaction and snapshots the affected rows. A visited set keyed by entity and id ends reference cycles. Rows that are cascade-deleted are never also reported as set-null, so the result does not depend on traversal order. After the `afterDelete` rules, `auditCascadeAsync` appends set-null changes as `update` rows, then cascaded deletes children first, then the root delete. Cascaded rows carry `caused_by` set to `entity/id` of the root. All rows join the same transaction, so a failed audit insert rolls back the whole delete. A `Restrict` reference makes the database reject the delete; the transaction rolls back, nothing is audited, and the middleware returns HTTP 409 with `MTPR-2009`.

Access denials (HTTP 403 for entity grants, field writes and field reads) are appended by `AuditTrail` as `denied` rows with an empty record ID and the reason in `detail`, for example `entity:create`, `field-write:PATCH` or `field-read:query`. `AuditTrail` uses its own connection and transaction because the request has no data transaction. If the append fails the request fails instead of silently allowing an unlogged denial.

Every row also stores `request_id`, taken from `HttpContext.TraceIdentifier`, so rows of one request can be correlated. `AuditTable.EnsureAsync` creates the table, adds missing `subject_issuer`, `request_id`, `caused_by` and `detail` columns, and then creates the triggers `__mtpr_audit_no_update` and `__mtpr_audit_no_delete`, which abort with "The audit log is append-only." The columns are upgraded first because adding a column is not an update. The check runs on every append inside the caller's transaction.

Limits: the plan is computed before the database acts, so a concurrent writer between the two steps could make it differ, although SQLite serializes writers. The triggers stop ordinary SQL but not someone who drops them or replaces the file, so the log is append-only rather than tamper-proof. Reads, deletes of missing records, authentication failures (401) and writes rolled back by rule rejections are not recorded. There is no audit query API, retention policy or external sink. Row-level filters and tenant isolation remain open in M4.

## Current scope and tests

Row filters and tenant isolation are not implemented. All records are accessible to a user with the applicable entity read grant, and fields are limited only where field grants are configured. The existing definition schema's proposed roles are not the source of runtime permissions. Do not treat business rules as access controls.

Integration tests use isolated SQLite databases. JWT tests exercise the real bearer handler with a simulated discovery endpoint and signing keys, including valid and invalid issuer, audience, signature and lifetime cases. Entity authorization tests cover role, entity and operation grants and default denial. Test authentication is limited to the test host. These tests do not establish interoperability with a particular external identity provider or prove tenant isolation.
