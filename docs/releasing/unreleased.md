# Unreleased

[Back to index](index.md)

## M5 Alpha Release (in progress)

- Removed unused public utilities from `Mtpr.Core`, including builders, repositories, pagination and extension helpers, and their tests. Consumers of these APIs must migrate before upgrading.
- Added a helpdesk sample (`docs/examples/m5-helpdesk`) built purely from definitions, covered by integration tests, and a `Dockerfile` that runs the API with it.
- Added `tools/Mtpr.LocalTokenService`, a development-only token service that issues RS256 JWTs and serves OIDC discovery and JWKS so the secured API runs without an external identity provider. The signing key comes from configuration or an ignored local file; never commit it. Replace the placeholder client secrets before use.
- Added a README quickstart and completed the M5 build, testing, deployment, operations and monitoring guides. The container image documents mounting a definitions folder for changes without restart.

## M4 Security and Multi-Tenancy

- CRUD endpoints require a JWT bearer token. Configure `Mtpr:Authentication:Authority` (HTTPS) and `Mtpr:Authentication:Audience` before starting the API. Existing unauthenticated clients must obtain and send a token.
- Integration tests exercise OIDC discovery and verify valid tokens and rejection of invalid signatures, issuers, audiences, and token lifetimes using a simulated identity provider.
- Entity-level role grants control read, create, update and delete operations; access is denied by default. Optional field grants and row filters further restrict configured entities. Cascading deletes and set-null changes check permissions on affected entities.
- Committed record changes on managed SQLite tables write before/after audit snapshots in the same transaction, including cascades and independent SQL changes after migration. The documented audit and deployment boundaries apply.
- Audit entries include the subject's issuer when available; existing audit tables gain a nullable issuer column on the next successful write.
- Added tenant scoping and transactional same-tenant reference and cascade checks. Reference graphs mixing scoped and unscoped entities reject writes. The supported isolation deployment uses one API process per database; the audit log is not a complete security-event log.

## M3 business rules

- Added read-only computed decimal fields with bounded arithmetic expressions. Computed results are persisted on create and update; changing a definition does not backfill existing rows.
- Added configuration-driven lifecycle states with an initial state and transactional directed transitions. Existing records are not migrated when lifecycle definitions change.
- Added bounded configuration-driven rejection rules before and after create, update, and delete. SQLite transactions roll back rejected operations, including cascades. Rules run only for the directly requested entity; cascaded changes do not execute related entity rules.
- Fields can specify a literal `default` applied when a create request omits the field. Defaults are checked against field type and constraints when definitions load; explicit null is not replaced. Updates do not apply defaults.

## M2 relations and querying

- Existing entities can gain optional fields without losing data. Existing fields must stay unchanged and in order; required additions remain unsupported.
- Added SQLite-enforced references with restrict, cascade and set-null delete behavior, plus composite uniqueness for join entities.
- Added list filtering, sorting, paging and field selection, and a configuration-only library sample.

## M1 vertical slice

- Added file-defined entities, SQLite table creation and a migration log, and generic REST CRUD endpoints.
- Added metadata-driven validation and MTPR-coded API errors.
- Added SQLite integration tests and xUnit test discovery for the solution.

## M0 - Foundation Hardening

- Added SDK selection, shared build settings and a GitHub Actions build and test workflow.
- Documented the v0 entity format, proposed role and rule shapes, initial architecture decisions and measurable MVP acceptance criteria. Runtime schema validation and enforcement of roles and rules are not included.
