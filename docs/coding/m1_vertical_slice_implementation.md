# M1 vertical slice implementation

- [Back to index](index.md)

This guide describes the code paths behind M1. For configuration and usage, see the build guide; for runnable checks, see the testing guide.

## Project responsibilities

| Project | Responsibility |
| --- | --- |
| Mtpr.Api | Configures the host, exposes generic REST endpoints, reads request bodies and maps engine errors to HTTP responses. |
| Mtpr.Engine | Loads and verifies definitions, coordinates requests, validates data and persists it in SQLite. |
| Mtpr.Core | Provides the shared `MtprException` and error-code abstractions. |
| Mtpr.IntegrationTests | Hosts the API with a temporary definitions directory and SQLite database. |

## Request flow

`Program.cs` maps endpoints under `/api/{entity}`. A create request follows this path:

1. `BodyReader` checks that the body is an object and builds a case-insensitive dictionary of JSON values; duplicate property names are rejected.
2. `EntityService.CreateAsync` gets the entity definition from `IDefinitionCatalog`.
3. The catalog refreshes definitions and applies schema changes if the files have changed, then returns the requested definition.
4. `IRecordValidator` validates and converts the input according to field metadata.
5. `IRecordStore` inserts the values and returns the created record. The endpoint returns HTTP 201 and a location for that record.

Read and delete operations resolve the definition and use the store. Update also validates input: PUT performs full validation, while PATCH permits omitted required fields and leaves omitted fields unchanged. For PUT, omitted optional fields are set to null. The list endpoint defaults to `skip=0` and `take=50`; the service clamps `skip` to at least zero and `take` to 1–200.

## Definitions and catalog

`JsonDirectoryDefinitionSource` reads one entity from each `.json` file and uses a SHA-256 hash of file contents and file paths as its change fingerprint. `EntityDefinitionVerifier` checks identifiers, duplicate and reserved field names, field types and applicable constraints. Entity and field lookup are case-insensitive. An invalid definition produces `MTPR-3001`; a bad file can prevent the catalog refresh for other entities too.

`DefinitionCatalog` is registered as a singleton. It checks the fingerprint on requests and reloads definitions when it changes. A `SemaphoreSlim` allows only one refresh at a time within the process. After entering the gate, the catalog checks the fingerprint again because another request may already have refreshed it. Definitions and the loaded fingerprint are replaced only after migration succeeds. The gate is not shared across separate processes.

## Persistence and migration

`SqliteSchemaMigrator` creates missing entity tables and records each applied definition's hash in `__mtpr_definitions`. Applying a batch of definitions and recording its hashes occur in one transaction. An applied definition with a different hash is rejected with `MTPR-3002`; M1 does not alter or drop existing tables. Adding a new entity is supported without rebuilding or restarting the API.

`SqliteRecordStore` constructs SQL from verified entity and field names and passes record values as parameters. It assigns each new record a GUID `id`. `SqliteTypeMap` maps types to SQLite storage: integers and booleans use `INTEGER`, doubles use `REAL`, and strings, decimals, dates and GUIDs use `TEXT`. Decimal conversion uses invariant culture.

## Validation and errors

`RecordValidator` checks unknown fields, required values, JSON types, string lengths and numeric bounds. It collects violations in `EntityValidationException` rather than failing at the first invalid field. Field names in the returned values come from the definition.

`MtprExceptionMiddleware` translates engine exceptions into JSON responses. Validation failures return HTTP 422 with a top-level code and an `errors` array; missing entities or records return 404; invalid or changed definitions return 500. Other `MtprException` codes map to 400. Codes are declared in `MtprErrors`. Framework-level errors and exceptions outside this middleware's handled types are not guaranteed to use this response format.

## Registration and tests

`AddMtprEngine` registers the catalog, definition source, validator, migrator, store, connection factory and service as singletons. The API provides the definitions directory and SQLite connection string through configuration.

`VerticalSliceTests` exercises the API through `WebApplicationFactory` and a temporary SQLite file. The test factory provides isolated paths and removes them on disposal. The testing guide lists the covered scenarios and commands.

M1 is a single-provider prototype; it does not implement authentication, authorization, tenant isolation, relations or schema evolution. These limitations and future milestones are described in the roadmap.
