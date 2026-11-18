# Unreleased

[Back to index](index.md)

## M3 business rules (in progress)

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
