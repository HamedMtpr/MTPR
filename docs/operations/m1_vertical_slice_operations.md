# M1 vertical slice operations

- [Back to index](index.md)

## Configuration and startup

The M1 API reads `Mtpr:DefinitionsPath` and `Mtpr:ConnectionString` from application configuration. Environment variables use double underscores: `Mtpr__DefinitionsPath` and `Mtpr__ConnectionString`. Defaults are `definitions` under the application content root and `Data Source=mtpr.db` relative to the process working directory. Set explicit paths for deployed instances. See [deployment](../deployment/m1_vertical_slice_deployment.md) for a runnable example.

The process reloads definitions on a request when files change. It validates all definitions and applies new tables in a transaction before serving the requested entity. A new entity can therefore be introduced without restarting or rebuilding. The API user needs read access to definitions and read/write access to the database directory.

## Add an entity

1. Prepare a JSON file using the [definition format](../building/m1_vertical_slice_build.md). Give it a unique entity name and do not modify an existing applied definition.
2. Place the completed `.json` file in the configured definitions directory. Avoid leaving a partially written JSON file in that directory while requests are being served.
3. Request `GET /api/{entity}`. A 200 response confirms the definition was loaded and its table created. Use the CRUD checks in the [testing guide](../testing/m1_vertical_slice_testing.md) if needed.

The migration log is stored in the database. Editing an applied definition is rejected with `MTPR-3002`; M1 does not migrate its schema. Do not delete the log or database to force a change on an instance containing data. Schema evolution is planned for M2.

## Diagnose failures

| Symptom | Action |
| --- | --- |
| `MTPR-1001` (404) | Check the entity name and the configured definitions directory. |
| `MTPR-1002` (404) | Check the entity and record ID. |
| `MTPR-2000` (422) | Inspect the individual codes and messages in `errors`; correct the request data. |
| `MTPR-3001` (500) | Fix the invalid definition identified in the response or application logs. A bad definition can prevent the entire catalog from reloading. |
| `MTPR-3002` (500) | Restore the applied definition content; modifying an applied definition is unsupported in M1. |
| Database or file access failure | Check configured paths, directory permissions, available disk space and process logs. |

The middleware logs `MtprException` failures as warnings. Collect the application process output through the host's logging mechanism. A successful request to a known entity checks the end-to-end path; M1 does not implement a dedicated health endpoint.

## Backup and recovery

Back up the database and the matching definition files together. Stop the API and any other writers before copying the SQLite database and definitions, and restore both from the same backup set before restarting. Verify the restored instance with a read request to a known entity. Practice recovery on a separate copy before relying on a backup procedure.
