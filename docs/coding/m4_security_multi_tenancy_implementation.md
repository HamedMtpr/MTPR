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
- [Audit tests](../../tests/Mtpr.IntegrationTests/Security/AuditTests.cs)
- [SQLite record store](../../src/Mtpr.Engine/Persistence/SqliteRecordStore.cs)

## Authentication

The API registers ASP.NET Core JWT bearer authentication and requires an authenticated principal on the `/api/{entity}` group. Startup requires an absolute HTTPS `Mtpr:Authentication:Authority` and a nonempty `Mtpr:Authentication:Audience`. The bearer handler uses the authority's OIDC discovery metadata and signing keys; it validates the token signature, issuer, audience and lifetime. Discovery requires HTTPS. Requests without a valid bearer token receive HTTP 401.

## Entity authorization

`EntityPermissions` reads `Mtpr:Authorization:Roles` once at startup. Each role contains entity names mapped to arrays of operations. Roles are evaluated with `ClaimsPrincipal.IsInRole`; the identity provider must issue a role claim that the bearer handler maps to the principal's role claim type. Role values use the principal's case-sensitive role matching. Entity names are compared case-insensitively, and `*` matches every entity. Operations are case-sensitive and must be `read`, `create`, `update`, or `delete`; an invalid operation in an entity's array prevents startup.

The endpoint filter maps list and single-record GET to `read`, POST to `create`, PUT and PATCH to `update`, and DELETE to `delete`. Any matching grant from any role allows the operation. Without a matching grant the filter returns HTTP 403 before the entity service runs. Permissions do not implicitly grant other operations. Restart the API after changing grants; definition files follow their separate reload behavior.

These checks protect the HTTP endpoint group; direct calls to the engine or database do not pass through the filter. Database cascades and set-null actions on related records do not check the affected entity's grants. Create and update responses can include fields even when the caller does not have the entity's `read` grant.

## Transactional audit records

The SQLite record store creates `__mtpr_audit` on the first successful direct write. Create, update and delete append one entry after their rules succeed and before the same transaction commits. If audit insertion fails, the data write rolls back. Each row contains an incrementing sequence, UTC timestamp, entity, record ID, operation, optional subject, and JSON snapshots before and after the operation. A successful update is recorded even if its values did not change. Reads and failed or rejected writes do not append an entry.

For API requests, `HttpAuditActorAccessor` obtains the principal's name-identifier claim; a token without that claim produces a null subject. Other engine hosts can register an `IAuditActorAccessor`; the default reports a null subject. The stored subject has no accompanying issuer and must not be assumed globally unique. Snapshots contain all record fields and may include sensitive values. The audit table is in the same database and is neither tamper-proof nor independently retained.

Only the directly requested engine operation is recorded. SQLite cascaded deletes and set-null changes, direct database writes, schema changes, reads, denied access and failed requests are not recorded. There is no audit query API, retention policy or tamper detection. Audit coverage is therefore incomplete and the M4 audit milestone remains open.

## Current scope and tests

Field-level authorization, row filters and tenant isolation are not implemented. All records and fields are accessible to a user with the applicable entity operation grant. The existing definition schema's proposed roles are not the source of runtime permissions. Do not treat business rules as access controls.

Integration tests use isolated SQLite databases. JWT tests exercise the real bearer handler with a simulated discovery endpoint and signing keys, including valid and invalid issuer, audience, signature and lifetime cases. Entity authorization tests cover role, entity and operation grants and default denial. Test authentication is limited to the test host. These tests do not establish interoperability with a particular external identity provider or prove tenant isolation.
