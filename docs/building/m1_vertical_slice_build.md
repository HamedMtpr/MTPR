# M1 vertical slice build

[Back to index](index.md)

## Related documentation

- [M1 deployment](../deployment/m1_vertical_slice_deployment.md)
- [M1 operations](../operations/m1_vertical_slice_operations.md)
- [M1 testing](../testing/m1_vertical_slice_testing.md)
- [M2 schema evolution](m2_schema_evolution.md) (current behavior after M1)
- [Unreleased changes](../releasing/unreleased.md)

Requires the .NET 10 SDK. From the repository root, run:

```sh
dotnet run --project src/Mtpr.Api
```

The URL is printed by the host. The API reads JSON files from `src/Mtpr.Api/definitions` by default; `Mtpr:DefinitionsPath` and `Mtpr:ConnectionString` can be overridden with configuration (for example, environment variables `Mtpr__DefinitionsPath` and `Mtpr__ConnectionString`). The SQLite database is created on first use.

Each file describes one entity. For example, add `category.json` while the server is running:

```json
{ 
   "name": "category", 
   "fields": [
   { 
      "name": "label", 
      "type": "string", 
      "required": true, 
      "minLength": 2, 
      "maxLength": 80 
   }] 
}
```

The next request reloads definitions and creates a table transactionally. No rebuild or restart is needed. Entity and field identifiers start with a letter and contain only letters, digits and underscores; `id` is reserved. Supported field types are `string`, `int`, `long`, `decimal`, `double`, `bool`, `dateTime` and `guid`. Numeric fields support `min` and `max`; strings support `minLength` and `maxLength`.

Use `POST /api/category` with `{"label":"Books"}`, `GET /api/category`, and `GET`, `PUT`, `PATCH`, or `DELETE /api/category/{id}`. `PUT` replaces all fields; `PATCH` updates only supplied fields. List supports `skip` and `take`. Validation responses contain `code`, `message` and an `errors` array with individual MTPR codes.

M1 only creates tables for new entities. Changes to a definition already applied to the database are rejected with `MTPR-3002`; schema evolution is planned for M2. Invalid definitions also prevent reloading the catalog until fixed. This prototype does not provide authentication or tenant isolation.

Run `dotnet test MTPR.slnx` to execute unit and integration tests. The integration tests use a real temporary SQLite database.
