# M1 vertical slice testing

- [Back to index](index.md)

## Automated tests

From the repository root with the .NET 10 SDK installed:

```sh
dotnet test MTPR.slnx
dotnet test tests/Mtpr.IntegrationTests/Mtpr.IntegrationTests.csproj
```

The second command runs the M1 integration suite alone. It hosts the actual API with a temporary definitions directory and a real SQLite file; each test creates its own isolated factory. The suite currently covers:

| Scenario | Expected result |
| --- | --- |
| Create, fetch, patch, list and delete a product | Created data is returned and persisted; fetching after deletion returns 404. |
| Invalid title, price and unknown field | HTTP 422 with three validation errors. |
| Unknown entity | HTTP 404. |
| Add a definition after the host has served a request | The new entity accepts a create request without a restart. |
| PATCH and PUT on an optional field | PATCH preserves an omitted field; PUT clears it. |
| Duplicate request properties differing only by case | HTTP 400 and `MTPR-2008`. |
| Definition missing a field type | `MTPR-3001`. |
| Change an applied definition | HTTP 500; M1 rejects the change. |

## Manual end-to-end scenarios

Start the API as described in the deployment guide. The examples below use the sample product definition and PowerShell's `curl.exe`; keep the API running throughout. Use an isolated database and definitions directory because these checks write data.

1. Create a record and note the `id` returned in the JSON response:

   ```powershell
   curl.exe -i -X POST http://127.0.0.1:5080/api/product -H 'Content-Type: application/json' --data '{\"title\":\"Pen\",\"price\":1.5}'
   ```

   Expect HTTP 201, a `Location` header and the created record.

2. Set `$id` to that ID. Fetch, update, list and delete it:

   ```powershell
   $id = '<id from create response>'
   curl.exe -i "http://127.0.0.1:5080/api/product/$id"
   curl.exe -i -X PATCH "http://127.0.0.1:5080/api/product/$id" -H 'Content-Type: application/json' --data '{\"price\":2}'
   curl.exe -i http://127.0.0.1:5080/api/product
   curl.exe -i -X DELETE "http://127.0.0.1:5080/api/product/$id"
   curl.exe -i "http://127.0.0.1:5080/api/product/$id"
   ```

   Expect the fetched and updated record, a list containing it, HTTP 204 on deletion, then HTTP 404 with `MTPR-1002`.

3. Submit an invalid product:

   ```powershell
   curl.exe -i -X POST http://127.0.0.1:5080/api/product -H 'Content-Type: application/json' --data '{\"title\":\"x\",\"price\":-1,\"extra\":1}'
   ```

   Expect HTTP 422, `MTPR-2000`, and violations for minimum length (`MTPR-2004`), minimum value (`MTPR-2005`) and the unknown field (`MTPR-2007`).

4. While the process is running, add a new entity file as described in operations, then send a create request to its endpoint. Expect HTTP 201 without a rebuild or restart. In a disposable test environment, change an already applied definition and request an entity: expect HTTP 500 with `MTPR-3002`; restore the original file afterwards.

The automated tests and these manual checks are complementary. The manual checks exercise the published deployment and its configured filesystem paths.
