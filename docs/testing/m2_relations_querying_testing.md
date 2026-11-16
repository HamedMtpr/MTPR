# M2 relations and querying testing

- [Back to index](index.md)

## Related Documents

- [M2 build guide](../building/m2_relations_querying.md)
- [M2 deployment](../deployment/m2_relations_querying_deployment.md)
- [M2 operations](../operations/m2_relations_querying_operations.md)
- [Integration tests](../../tests/Mtpr.IntegrationTests/)
- [Library sample definitions(author.json)](../examples/m2-library/author.json)
- [Library sample definitions(book.json)](../examples/m2-library/book.json)
- [Library sample definitions(booktag.json)](../examples/m2-library/abooktag.json)
- [Library sample definitions(tag.json)](../examples/m2-library/tag.json)

## Automated tests

Run from the repository root with the installed .NET SDK:

```powershell
dotnet test MTPR.slnx
```

Run the integration project separately when investigating API or SQLite behavior:

```powershell
dotnet test tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj
```

The integration tests host the API with temporary definition files and a real temporary SQLite database. The M2 suite covers the library flow, reference validation, restrict, cascade and set-null deletes, duplicate pairs, filters, sorting, paging, field selection, decimal precision and optional-reference migration. It also checks ordering when an entity has a `rowid` field. The library sample test uses the JSON definitions under `docs/examples/m2-library`; keep these files with the test project in a checkout.

## Manual verification

Use a disposable definitions directory and database. Start the API with the library sample as described in the M2 deployment guide. Follow the sample API flow in the build guide and check:

1. A book with an unknown author is rejected with HTTP 409 and `MTPR-2009`.
2. Adding the same book-tag pair twice returns HTTP 409. Deleting the book removes the join records but leaves the tag.
3. Deleting an author referenced by a book returns HTTP 409. Use an optional reference configured with `setNull` to verify that deletion clears the reference.
4. Filter books by `authorId`, sort and page results, and select a subset of fields. Invalid fields or filter values return HTTP 400 and `MTPR-2010`.
5. Create a record, append an optional field to its definition, then fetch it after reload. Existing values remain and the new field is `null`. An unsupported change returns `MTPR-3002`.

The sample flow and automated tests do not establish that all branches or concurrent scenarios are covered. A coverage report measures the run used to produce it; inspect its scope and date before applying its figures to another revision.
