# M4 security and multi-tenancy: implementation

- [Back to index](index.md)
- [M4 build and run](../building/m4_security_multi_tenancy_build.md)
- [M4 deployment](../deployment/m4_security_multi_tenancy_deployment.md)
- [M4 operations](../operations/m4_security_multi_tenancy_operations.md)
- [M4 testing](../testing/m4_security_multi_tenancy_testing.md)
- [Tenant policy](../../src/Mtpr.Api/Authorization/TenantPolicy.cs)
- [Tenant isolation tests](../../tests/Mtpr.IntegrationTests/Security/Tenancy/TenantIsolationTests.cs)
- [Tenant relation tests](../../tests/Mtpr.IntegrationTests/Security/Tenancy/TenantRelationTests.cs)
- [Tenant scoped relation tests](../../tests/Mtpr.IntegrationTests/Security/Tenancy/TenantScopedRelationsTests.cs)
- [Definition reload tests](../../tests/Mtpr.IntegrationTests/Security/Tenancy/TenantDefinitionReloadTests.cs)
- [Tenant transaction checks](../../src/Mtpr.Engine/Persistence/SqliteRecordStore_Tenancy.cs)
- [API entry point](../../src/Mtpr.Api/Program.cs)
- [Entity permissions](../../src/Mtpr.Api/EntityPermissions.cs)
- [JWT validation tests](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests.cs)
- [Entity authorization tests](../../tests/Mtpr.IntegrationTests/Security/EntityAuthorizationTests.cs)
- [Field write permissions](../../src/Mtpr.Api/Authorization/FieldWritePermissions.cs)
- [Field grants](../../src/Mtpr.Api/Authorization/FieldGrants.cs)
- [Field read permissions](../../src/Mtpr.Api/Authorization/FieldReadPermissions.cs)
- [Field read tests](../../tests/Mtpr.IntegrationTests/Security/FieldReads/FieldReadTests.cs)
- [Field write tests](../../tests/Mtpr.IntegrationTests/Security/FieldWrites/FieldWriteTests.cs)
- [Indirect write authorization](../../src/Mtpr.Api/Authorization/HttpRecordWriteAuthorization.cs)
- [Indirect write tests](../../tests/Mtpr.IntegrationTests/Security/IndirectWrites/IndirectWriteTests.cs)
- [Audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests.cs)
- [Audit upgrade tests](../../tests/Mtpr.IntegrationTests/Security/Audits/AuditTests_Legacy.cs)
- [JWT audit test](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests_Audit.cs)
- [SQLite record store](../../src/Mtpr.Engine/Persistence/SqliteRecordStore.cs)
- [Cascade planning](../../src/Mtpr.Engine/Persistence/SqliteRecordStore_Cascade.cs)
- [Audit table](../../src/Mtpr.Engine/Persistence/Audit/AuditTable.cs)
- [External SQL audit triggers](../../src/Mtpr.Engine/Persistence/Audit/SqliteExternalAudit.cs)
- [External SQL audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/ExternalSql/ExternalAuditTests.cs)
- [Audit entry](../../src/Mtpr.Engine/Persistence/Audit/AuditEntry.cs)
- [Audit trail for denials](../../src/Mtpr.Engine/Persistence/Audit/AuditTrail.cs)
- [Audit actor accessor](../../src/Mtpr.Api/HttpAuditActorAccessor.cs)
- [Cascade audit tests](../../tests/Mtpr.IntegrationTests/Security/Audits/Cascades/AuditTests_Cascade.cs)

## Authentication

The API registers ASP.NET Core JWT bearer authentication and requires an authenticated principal on the `/api/{entity}` group. Startup requires an absolute HTTPS `Mtpr:Authentication:Authority` and a nonempty `Mtpr:Authentication:Audience`. The bearer handler uses the authority's OIDC discovery metadata and signing keys; it validates the token signature, issuer, audience and lifetime. Discovery requires HTTPS. Requests without a valid bearer token receive HTTP 401.

## Entity authorization

`EntityPermissions` reads `Mtpr:Authorization:Roles` (reloaded at runtime, see "Runtime reload of policies"). Each role contains entity names mapped to arrays of operations. Roles are evaluated with `ClaimsPrincipal.IsInRole`; the identity provider must issue a role claim that the bearer handler maps to the principal's role claim type. Role values use the principal's case-sensitive role matching. Entity names are compared case-insensitively, and `*` matches every entity. Operations are case-sensitive and must be `read`, `create`, `update`, or `delete`; an invalid operation in an entity's array prevents startup.

The endpoint filter maps list and single-record GET to `read`, POST to `create`, PUT and PATCH to `update`, and DELETE to `delete`. Any matching grant from any role allows the operation. Without a matching grant the filter returns HTTP 403 before the entity service runs. Permissions do not implicitly grant other operations. Reloadable configuration applies to subsequent requests without restarting.

These checks protect the HTTP endpoint group; direct calls to the engine or database do not pass through the filter. Create and update responses are governed by field read grants even when the caller does not have the entity's `read` grant.

## Field write grants

`Mtpr:Authorization:FieldWrites` optionally lists allowed input fields by entity and role. A configured entity denies writes from roles without a field grant. Matching role grants are combined, field and entity names are case-insensitive, and `*` in a role's field list grants all input fields for that entity. An entity with no field policy retains the existing entity-level behavior. The policy reloads at runtime without a restart. A wildcard entity is not supported.

`Mtpr:Authorization:FieldReads` uses the same shape (`FieldGrants` parses both) to list readable output fields by entity and role. For a configured entity the endpoint filter in `Program.cs` first rejects with HTTP 403 any list request whose `fields`, `sort` or `filter` names a field that is not granted, because ordering or filtering by a hidden field would reveal its values. `id` is always readable. After the handler runs, `FieldReadPermissions.Filter` projects the record or records of GET, POST, PUT and PATCH responses to granted fields. A restricted entity returning an unexpected value result throws instead of leaking it. Without a field read grant, list queries are denied and individual record responses contain only `id`. Entities without a policy are unchanged.

The endpoint filter first requires the entity operation grant. On POST and PATCH, every property in an object body must be in the caller's field grants; otherwise the request returns HTTP 403. On PUT the caller must have the `*` field grant, because omitted fields can also be modified. A body that is not a JSON object (for example an array or a missing body) is rejected with HTTP 403 on configured entities. The normal engine validation continues to reject unknown, duplicate and computed input fields.

The store also checks the actual non-null fields of a created record and the changed fields of an updated record before committing. This includes defaults, lifecycle initial values, computed values and tenant stamps. Grant each such field, or `*`, to roles that must perform the write. A delete requires `delete` on each entity affected by a cascade; each set-null requires `update` on the related entity and a write grant for the changed field. An indirect denial rolls back the complete transaction and is audited as `field-write:indirect`. The root request's entity and field policies are captured once and used throughout the request. Field policies are optional per entity; absence leaves all fields available to an entity-authorized caller. Direct database access and engine hosts using the default unrestricted authorization bypass HTTP policies. Field projection does not prevent inference through computed values, validation messages or business rules; protect sensitive derived data and database access separately.

## Transactional audit records

The schema migrator creates or upgrades `__mtpr_audit` and installs audit triggers when it loads definitions. Record-store create, update and delete append one entry after their rules succeed and before the same transaction commits. If audit insertion fails, the data write rolls back. Each row contains an incrementing sequence, UTC timestamp, entity, record ID, operation, optional subject and subject issuer, and JSON snapshots before and after the operation. A successful update is recorded even if its values did not change. Reads and failed or rejected writes do not append a change entry.

For API requests, `HttpAuditActorAccessor` obtains the principal's name-identifier and `iss` claims; missing claims produce null values. The normal bearer handler validates the token issuer before a write reaches the engine. Other engine hosts can register an `IAuditActorAccessor`; the default reports null values, and custom accessors are responsible for their own identity provenance. The optional issuer distinguishes equal subjects from different issuers, but neither field is guaranteed to exist. Snapshots contain all record fields and may include sensitive values. The audit table is in the same database and is neither tamper-proof nor independently retained.

An existing audit table without `subject_issuer` receives the nullable column during the next definition migration transaction. Existing rows retain their values and have a null issuer; it is not possible to infer their historical issuer. The default interface implementation returns null for issuer so existing custom accessors can continue to operate.

### Independent SQLite writes

After definitions have been loaded, managed entity tables have SQLite triggers for independent INSERT, UPDATE and DELETE, including cascades performed by that connection. These append in the same transaction and fail the data statement if the audit insert fails. They store `external-sql` in `detail`; subject, issuer, request and cause are unknown. Snapshots reflect SQLite storage values: booleans are integers and decimals are text. External clients must enable foreign keys and register `MTPR_DECIMAL` when required by the schema. API record-store transactions use a transactional marker to suppress these triggers and retain their existing typed snapshots, actor and cascade attribution; rollback also rolls back the marker. The marker table must remain empty outside those transactions.

SQLite `REPLACE` can delete conflicting rows without firing delete triggers. Guard triggers therefore abort conflicting inserts and updates, including `INSERT OR REPLACE`, `UPDATE OR REPLACE`, conflicting `OR IGNORE` and UPSERT. Use explicit update or delete operations instead. Trigger definitions are refreshed with schema migrations. Direct SQL bypasses validation, authorization, business rules and tenant checks, even though its record changes are logged.

The guarantee covers committed row changes on managed entity tables after migration, with the audit and guard triggers intact. It does not cover prior changes, other tables, DDL, file replacement, database-owner tampering or writes that disable or remove the triggers or marker safeguards. SQLite does not authenticate external SQL users; the log cannot identify them. Protect database and schema access and use an independent audit system if tamper resistance or comprehensive security-event logging is required.

### Cascades, denials and integrity

`SqliteSchemaMigrator` emits `ON DELETE CASCADE`, `SET NULL` or `RESTRICT` clauses and the connection enables `foreign_keys`, so SQLite itself performs these actions. The store leaves that to the database and only works out beforehand what it will touch. Before the root `DELETE`, `planCascadeAsync` walks the referencing fields breadth-first inside the delete transaction and snapshots the affected rows. A visited set keyed by entity and id ends reference cycles. Rows that are cascade-deleted are never also reported as set-null, so the result does not depend on traversal order. After the `afterDelete` rules, `auditCascadeAsync` appends set-null changes as `update` rows, then cascaded deletes children first, then the root delete. Cascaded rows carry `caused_by` set to `entity/id` of the root. All rows join the same transaction, so a failed audit insert rolls back the whole delete. A `Restrict` reference makes the database reject the delete; the transaction rolls back, nothing is audited, and the middleware returns HTTP 409 with `MTPR-2009`.

Access denials (HTTP 403 for entity grants, field writes and field reads) are appended by `AuditTrail` as `denied` rows with an empty record ID and the reason in `detail`, for example `entity:create`, `field-write:PATCH` or `field-read:query`. `AuditTrail` uses its own connection and transaction because the request has no data transaction. If the append fails the request fails instead of silently allowing an unlogged denial.

API rows also store `request_id`, taken from `HttpContext.TraceIdentifier`, so rows of one request can be correlated. `AuditTable.EnsureAsync` creates the table, adds missing `subject_issuer`, `request_id`, `caused_by` and `detail` columns, and then creates the triggers `__mtpr_audit_no_update` and `__mtpr_audit_no_delete`, which abort with "The audit log is append-only." The columns are upgraded first because adding a column is not an update. The check runs during migration and on every application append.

Limits: the plan is computed before the database acts, so a concurrent writer between the two steps could make it differ, although SQLite serializes writers. The triggers stop ordinary SQL but not someone who drops them or replaces the file, so the log is append-only rather than tamper-proof. Reads, deletes of missing records, authentication failures (401) and writes rolled back by rule rejections are not recorded. There is no audit query API, retention policy or external sink.

## Current scope and tests

Tenant isolation is implemented by `TenantPolicy` (`src/Mtpr.Api/Authorization`). Configure `Mtpr:Tenancy:ClaimType` (default `tenant_id`) and `Mtpr:Tenancy:Entities:{entity}` set to the entity's tenant field name. For a listed entity the API: denies (403, audited as `denied`) callers without the tenant claim; ANDs `{field}:eq:{tenant}` into every row-filter group, or uses it alone when no row filter exists, so a role cannot bypass it and out-of-tenant records return 404; overwrites the tenant field on create with the caller's tenant; and rejects with 403 any create or update body that sets the field to another tenant. Omitting the field on PUT or PATCH keeps the stored tenant. The tenant and row-filter policies are captured once for each request. The policy is a `ReloadablePolicy` and applies without a restart when the provider signals a reload. Invalid configuration (empty claim type, `*` entity key, empty field name) keeps the previous policy. Entities not listed are not tenant-scoped.

The tenant field must be an ordinary, noncomputed string and every declared uniqueness group must include it; requests using invalid definitions are denied. Every entity in a reference graph connected to a tenant entity must be tenant-scoped or writes in that graph are denied. For fully scoped graphs, referenced records are checked against the caller's tenant in the write transaction. Deletes check every cascaded and set-null record, as well as incoming restricted references, before changing data; an out-of-tenant reference rejects and rolls back the operation. Existing invalid cross-tenant references must be repaired before the affected delete can succeed.

The API holds a catalog lease and pins a definition snapshot for each request, including cascade planning. Definition refresh waits for active requests, avoiding a graph change between authorization and execution within one process. This implementation serializes API requests while the lease is held. Use a single API process for each database; separate processes and external tools do not share this in-memory lease. Direct database access and engine calls that omit scope bypass the API policy; audit snapshots are not tenant-filtered. Other row filters apply only where configured. Fields are limited only where field grants are configured.

Integration tests use isolated SQLite databases. JWT tests exercise the real bearer handler with a simulated discovery endpoint and signing keys, including valid and invalid issuer, audience, signature and lifetime cases. Entity authorization tests cover role, entity and operation grants and default denial. Tenant tests cover configured standalone entities and denial of writes through related entities. Test authentication is limited to the test host; tests do not establish interoperability with a particular external identity provider.

## Row-level filters

`Mtpr:Authorization:RowFilters:{entity}:{role}` lists `field:operator:value` filters (the list-query syntax). `RowFilterPermissions` resolves `{claim:type}` values from the caller's claims. Filters of one role are AND-ed and the groups of all matching roles are OR-ed. A missing claim makes that role's group unusable; an entity with a policy and no usable group matches no rows. Entities without a policy are unchanged. The policy reloads at runtime.

The list endpoint passes the groups in `RecordQuery.Scope`, which `SqliteQueryBuilder` adds to the `WHERE` clause with the caller's own filters, so a caller filter can only narrow the result. GET by id uses a scoped database query. PUT, PATCH and DELETE check visibility inside the same SQLite write transaction as the change; records outside the scope return HTTP 404 (`RecordNotFound`). POST and updates also check the final stored record against the scope in that transaction. If it is outside the scope, the write and its audit entries roll back and the response is HTTP 404.

Scope is passed explicitly to the engine; direct engine callers that omit it and direct database access bypass row filtering. Row filters are not applied to cascaded changes or audit snapshots. Removing an entity's row policy removes the restriction, and multiple matching roles widen access by union. Tenant checks for cascades are independent of row filters.

## Runtime reload of policies

`ReloadablePolicy<T>` builds the entity, field write, field read and row filter policies from `IConfiguration` and rebuilds them through `ChangeToken.OnChange` whenever configuration reloads, so changes apply to the next request without a restart. The new instance replaces the old one atomically. The first build at startup must succeed. If a later build is invalid, the error is logged, the previous valid policy stays active and requests continue to use it.

Only reloadable configuration sources trigger this: `appsettings*.json` (the default host enables `reloadOnChange`), other file providers with reload enabled, and providers that signal changes. Environment variables and command-line arguments are read once and need a restart. Each policy section is captured for the request; sections reload independently, so a request can observe versions from different reloads. JWT authority, audience and signing settings are not reloaded.

Setting an `IConfiguration` value programmatically does not itself signal a reload. Tests explicitly reload the configuration root after changing a value. Row-filter operators and basic syntax are checked on reload, while field existence and value types are validated when a query runs. An invalid reload keeps the previous policy, which may be less restrictive than the intended new one; inspect logs and verify the effective access after changes.
