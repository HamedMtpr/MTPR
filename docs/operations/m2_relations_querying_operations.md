# M2 relations and querying operations

- [Back to index](index.md)
- [M2 deployment](../deployment/m2_relations_querying_deployment.md)
- [M2 relations and querying](../building/m2_relations_querying.md)
- [M2 testing](../testing/m2_relations_querying_testing.md)
- [M1 operations](m1_vertical_slice_operations.md)

## Definition changes

The API checks definition files on requests. Prepare complete, valid files before placing them in the configured directory; the whole catalog is loaded before migrations run. Add referenced entities and their dependent definitions together. An invalid file or missing referenced entity can prevent catalog reload.

Append only optional fields to an existing entity. Existing fields and their order, references and unique groups must remain unchanged. Existing rows receive null for new fields. The schema and migration log are updated transactionally, but definition files are outside the transaction. Keep the last applied definitions so they can be restored after an unsupported change. Never remove the migration log or database to force a change on an instance containing data.

## Diagnose errors

| Response | Investigation |
| --- | --- |
| HTTP 400, `MTPR-2010` | Check filter syntax, operators, values, sort fields and selected fields. |
| HTTP 409, `MTPR-2009` | Check referenced records, delete behavior and uniqueness groups. |
| HTTP 500, `MTPR-3001` | Correct invalid definitions or missing target entities; a bad file can block the catalog. |
| HTTP 500, `MTPR-3002` | Restore the applied definition and make only supported additive changes. |

Application connections enable SQLite foreign keys and register the decimal collation. External tools do not automatically inherit these settings; avoid writes that bypass application validation or referential integrity. For a database with a unique decimal constraint, external SQLite clients must register a compatible `MTPR_DECIMAL` collation when accessing the relevant schema.

## Backup and recovery

Stop the API and other writers, then back up the SQLite database and matching definition files together. Restore both from the same backup set and verify a known entity and its relations after restarting. Test the recovery procedure on a separate copy. Do not replace a live database or definition set with a partial backup.
