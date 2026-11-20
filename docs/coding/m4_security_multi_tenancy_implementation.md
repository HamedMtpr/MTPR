# M4 security and multi-tenancy: implementation

- [Back to index](index.md)
- [M4 build and run](../building/m4_security_multi_tenancy_build.md)
- [M4 deployment](../deployment/m4_security_multi_tenancy_deployment.md)
- [M4 operations](../operations/m4_security_multi_tenancy_operations.md)
- [API entry point](../../src/Mtpr.Api/Program.cs)
- [Entity permissions](../../src/Mtpr.Api/EntityPermissions.cs)
- [JWT validation tests](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests.cs)
- [Entity authorization tests](../../tests/Mtpr.IntegrationTests/Security/EntityAuthorizationTests.cs)

## Authentication

The API registers ASP.NET Core JWT bearer authentication and requires an authenticated principal on the `/api/{entity}` group. Startup requires an absolute HTTPS `Mtpr:Authentication:Authority` and a nonempty `Mtpr:Authentication:Audience`. The bearer handler uses the authority's OIDC discovery metadata and signing keys; it validates the token signature, issuer, audience and lifetime. Discovery requires HTTPS. Requests without a valid bearer token receive HTTP 401.

## Entity authorization

`EntityPermissions` reads `Mtpr:Authorization:Roles` once at startup. Each role contains entity names mapped to arrays of operations. Roles are evaluated with `ClaimsPrincipal.IsInRole`; the identity provider must issue a role claim that the bearer handler maps to the principal's role claim type. Role values use the principal's case-sensitive role matching. Entity names are compared case-insensitively, and `*` matches every entity. Operations are case-sensitive and must be `read`, `create`, `update`, or `delete`; an invalid operation in an entity's array prevents startup.

The endpoint filter maps list and single-record GET to `read`, POST to `create`, PUT and PATCH to `update`, and DELETE to `delete`. Any matching grant from any role allows the operation. Without a matching grant the filter returns HTTP 403 before the entity service runs. Permissions do not implicitly grant other operations. Restart the API after changing grants; definition files follow their separate reload behavior.

These checks protect the HTTP endpoint group; direct calls to the engine or database do not pass through the filter. Database cascades and set-null actions on related records do not check the affected entity's grants. Create and update responses can include fields even when the caller does not have the entity's `read` grant.

## Current scope and tests

Field-level authorization, row filters, tenant isolation and audit logging are not implemented. All records and fields are accessible to a user with the applicable entity operation grant. The existing definition schema's proposed roles are not the source of runtime permissions. Do not treat business rules as access controls.

Integration tests use isolated SQLite databases. JWT tests exercise the real bearer handler with a simulated discovery endpoint and signing keys, including valid and invalid issuer, audience, signature and lifetime cases. Entity authorization tests cover role, entity and operation grants and default denial. Test authentication is limited to the test host. These tests do not establish interoperability with a particular external identity provider or prove tenant isolation.
