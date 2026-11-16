# Unreleased

[Back to index](index.md)

## M2 relations and querying

- Existing entities can gain optional fields without losing data. Existing fields must stay unchanged and in order; required additions remain unsupported.
- Added SQLite-enforced references with restrict, cascade and set-null delete behavior, plus composite uniqueness for join entities.
- Added list filtering, sorting, paging and field selection, and a configuration-only library sample.

## M1 vertical slice

- Added file-defined entities, SQLite table creation and a migration log, and generic REST CRUD endpoints.
- Added metadata-driven validation and MTPR-coded API errors.
- Added SQLite integration tests and xUnit test discovery for the solution.
