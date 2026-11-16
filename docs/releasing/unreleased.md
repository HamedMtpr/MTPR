# Unreleased

[Back to index](index.md)

## M2 in progress

- Existing entities can gain optional fields without losing data. Existing fields must stay unchanged and in order; required additions remain unsupported.

## M1 vertical slice

- Added file-defined entities, SQLite table creation and a migration log, and generic REST CRUD endpoints.
- Added metadata-driven validation and MTPR-coded API errors.
- Added SQLite integration tests and xUnit test discovery for the solution.
