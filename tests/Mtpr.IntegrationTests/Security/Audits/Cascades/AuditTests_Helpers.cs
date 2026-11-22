using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace Mtpr.IntegrationTests.Security.Audits.Cascades;

public partial class AuditTests
{
    private const string author = """
                                  {
                                      "name": "author",
                                      "fields": [ { "name": "name", "type": "string" } ]
                                  }
                                  """;

    private static string book(string onDelete) => $$"""
                                                     {
                                                         "name": "book",
                                                         "fields": [
                                                             { "name": "title", "type": "string" },
                                                             {
                                                                 "name": "authorId",
                                                                 "type": "guid",
                                                                 "references": { "entity": "author", "onDelete": "{{onDelete}}" }
                                                             }
                                                         ]
                                                     }
                                                     """;

    private static async Task<string> createAsync(HttpClient client,
        string entity,
        object body,
        CancellationToken ct)
    {
        using var response = await client.PostAsJsonAsync($"/api/{entity}", body, ct);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<JsonElement>(ct))
            .GetProperty("id")
            .GetString()!;
    }

    private static async Task<List<(string Entity, string Operation, string? CausedBy, string? Request, string? Detail)>>
        auditAsync(MtprApiFactory factory, CancellationToken ct)
    {
        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);
        await using var command = connection.CreateCommand();
        command.CommandText = """
                              SELECT entity, operation, caused_by, request_id, detail
                              FROM __mtpr_audit
                              ORDER BY sequence
                              """;

        List<(string, string, string?, string?, string?)> rows = [];
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
            rows.Add((reader.GetString(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4)));

        return rows;
    }
}