# M2 schema evolution

- [Back to index](index.md)
- [M1 vertical slice build](m1_vertical_slice_build.md)
- [M1 operations](../operations/m1_vertical_slice_operations.md)
- [Project roadmap (See M2 Milestone)](../planning/project_roadmap.md)

M2 is in progress. This document describes the additive schema changes currently implemented; relations and the remaining querying features are still planned.

## Add an optional field

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

## Limits and operations

- Required fields cannot be added to an existing entity. Removing, renaming, reordering, or changing an existing field or its constraints is unsupported. These changes result in `MTPR-3002`.
- The migrator compares the existing definition with the hash stored in `__mtpr_definitions` before applying an addition. Database schema changes and migration log updates are applied in one SQLite transaction; the definition files themselves are outside that transaction.
- A malformed or unsupported definition can prevent the catalog from reloading. Restore the last applied definition or correct the change before retrying. Do not delete the database or migration log to force a change on an instance containing data.
- Back up the database and its matching definitions together as described in [M1 operations](../operations/m1_vertical_slice_operations.md#backup-and-recovery).

## Verify

Create a record before adding the optional field, apply the definition change, and fetch the record again. Verify that its existing values are unchanged and that the new field is `null`; update the field and fetch it again. The integration tests also cover repeated optional additions and rejection of required additions. Run them with the installed .NET SDK:

```sh
dotnet test tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj
```
