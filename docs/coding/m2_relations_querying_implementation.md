# M2 relations and querying implementation

- [Back to index](index.md)
- [M2 relations and querying build guide](../building/m2_relations_querying.md)
- [M2 testing](../testing/m2_relations_querying_testing.md)
- [Entity definition](../../src/Mtpr.Engine/Definitions/EntityDefinition.cs)
- [Field reference](../../src/Mtpr.Engine/Definitions/FieldReference.cs)
- [SQLite schema migrator](../../src/Mtpr.Engine/Persistence/SqliteSchemaMigrator.cs)
- [SQLite query builder](../../src/Mtpr.Engine/Persistence/SqliteQueryBuilder.cs)

## Definitions and migration

`FieldDefinition.References` identifies the target entity and delete behavior for a GUID field. The verifier checks the field type, target identifier and delete behavior; the definition source checks that the target entity is in the loaded catalog. `EntityDefinition.Unique` lists groups of fields with a composite uniqueness constraint. A join entity with two required references and a unique group models a many-to-many relation.

The catalog loads and validates definitions before the SQLite migrator applies them. New tables receive foreign keys and unique constraints. An existing entity can gain optional fields, including optional references, but changes to existing fields, references and uniqueness groups are rejected. The migrator verifies the stored definition hash and applies schema changes and log updates in a transaction. Definition files are not part of that transaction.

The connection factory enables SQLite foreign keys on each application connection. It also registers `MTPR_DECIMAL` so decimal comparisons, ordering and unique constraints use .NET decimal comparison. A direct SQLite connection must configure its own foreign key enforcement and collation; the application connection settings do not apply to external clients.

## Queries and errors

The list endpoint builds a `RecordQuery` from paging, filtering, sorting and field selection parameters. The service clamps paging; `SqliteQueryBuilder` resolves field names against the definition and binds filter values as SQL parameters. Multiple filters are combined with AND. The store reads the rows and applies the requested field selection to the response. The internal SQLite `_rowid_` breaks sorting ties and supplies the default order.

Malformed query values return `MTPR-2010` with HTTP 400. Foreign key and unique constraint failures return `MTPR-2009` with HTTP 409. Invalid definitions and unsupported changes return `MTPR-3001` and `MTPR-3002` respectively. The middleware maps these errors; the testing guide covers observable behavior.
