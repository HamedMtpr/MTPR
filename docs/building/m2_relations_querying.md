# M2 relations and querying

- [Back to index](index.md)
- [Project roadmap](../planning/project_roadmap.md)
- [Library sample: author](../examples/m2-library/author.json)
- [Library sample: book](../examples/m2-library/book.json)
- [Library sample: tag](../examples/m2-library/tag.json)
- [Library sample: book-tag association](../examples/m2-library/booktag.json)
- [M1 build guide](m1_vertical_slice_build.md)
- [M1 operations](../operations/m1_vertical_slice_operations.md)

## Relations

Define a one-to-many relation using a `guid` field with `references.entity` naming the parent entity. The referenced value is the parent's `id`. The referenced entity must be present in the definitions directory. References are enforced by SQLite on every application connection, including updates and deletes. Invalid references and uniqueness conflicts return HTTP 409 with code `MTPR-2009`.

`references.onDelete` supports `restrict` (the default), `cascade`, and `setNull`. `setNull` requires an optional field. An existing table may gain a new optional reference as an additive migration; changing an existing reference or adding a required reference to an existing table is unsupported.

Model a many-to-many relation with a separate entity containing two required reference fields. Define `unique` as an array of field-name arrays to prevent duplicate pairs, for example `"unique": [["bookId", "tagId"]]`. Unique groups are defined when the entity is created; adding or changing them on an applied entity is unsupported. As in SQLite, null values do not conflict in a unique group. Create, list and delete relationship records through the normal CRUD endpoints.

## Querying

`GET /api/{entity}` retains `skip` (default 0) and `take` (default 50, maximum 200). It additionally accepts:

- `filter=field:operator:value`, repeated for multiple filters combined with AND. Operators are `eq`, `ne`, `gt`, `ge`, `lt`, `le`, and `contains` (strings only). `isnull` and `notnull` use an empty value, for example `filter=authorId:isnull:`. Values use invariant number formatting; dates use ISO 8601 and booleans use `true` or `false`. URL-encode values as needed.
- `sort=field,-otherField` for ascending and descending ordering. Equal values are ordered by insertion order. Without a sort, insertion order is used.
- `fields=field1,field2` to select the returned fields, including `id` only if explicitly requested.

Field names are checked against the entity definition and filter values are bound as parameters. Invalid fields, operators or values return HTTP 400 with code `MTPR-2010`. String `contains` is case-sensitive. Decimal comparisons and ordering use .NET decimal precision; other SQLite clients that access a schema containing a unique decimal field need the application's `MTPR_DECIMAL` collation.

For example: `GET /api/book?filter=price:ge:10&sort=-price,title&skip=0&take=20&fields=id,title,price`.

## Schema evolution

An existing entity definition can gain optional fields without rebuilding or restarting the API. Keep all existing fields unchanged and in their original order, and append the new fields. For example, after the original `category` definition from the M1 guide has been applied, append `description`:

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
    },
    { 
       "name": 
       "description", 
       "type": "string" 
    }
  ]
}
```

Write the complete definition to the configured definitions directory. On the next request the catalog reloads and the migrator adds the column. Existing rows retain their values and the new field is `null` until updated. Multiple optional fields may be appended in one change, and additional optional fields can be appended later. New entities can also be added as in M1.

- Required fields cannot be added to an existing entity. Removing, renaming, reordering, or changing an existing field or its constraints is unsupported. These changes result in `MTPR-3002`.
- The migrator compares the existing definition with the hash stored in `__mtpr_definitions` before applying an addition. Database schema changes and migration log updates are applied in one SQLite transaction; the definition files themselves are outside that transaction.
- A malformed or unsupported definition can prevent the catalog from reloading. Restore the last applied definition or correct the change before retrying. Do not delete the database or migration log to force a change on an instance containing data.
- Back up the database and its matching definitions together as described in M1 operations.

To verify a migration, create a record before adding the optional field, apply the definition change, and fetch the record again. Check that its existing values are unchanged and that the new field is `null`; update the field and fetch it again. The integration tests also cover repeated optional additions and rejection of required additions.

```sh
dotnet test tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj
```

## Sample library domain

The library definitions describe authors and books (one-to-many), tags and books (many-to-many through `booktag`). Configure `Mtpr:DefinitionsPath` to the absolute path of their directory, use a new SQLite database, and start the API following the M1 build guide.

1. `POST /api/author` with `{"name":"Ada"}` and save the returned `id` as `authorId`.
2. `POST /api/book` with `{"title":"Algorithms","price":12.5,"authorId":"<authorId>"}` and save the returned `id` as `bookId`.
3. `POST /api/tag` with `{"name":"computing"}` and save the returned `id` as `tagId`.
4. `POST /api/booktag` with `{"bookId":"<bookId>","tagId":"<tagId>"}`. A duplicate pair returns 409.
5. Find the author's books using `GET /api/book?filter=authorId:eq:<authorId>` and a book's tags via `GET /api/booktag?filter=bookId:eq:<bookId>`.
6. Deleting the author is rejected while the book exists. Deleting the book cascades to its `booktag` records; the tag remains.

The integration suite exercises this kind of multi-entity flow, filtering, paging, sorting, projections, reference validation, delete behavior, and additive reference migrations. Run `dotnet test MTPR.slnx` with the installed .NET SDK.
