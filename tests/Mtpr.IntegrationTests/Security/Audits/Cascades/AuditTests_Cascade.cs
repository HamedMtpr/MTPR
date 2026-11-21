using System.Net;
using System.Net.Http.Json;
using Microsoft.Data.Sqlite;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Audits.Cascades;

public partial class AuditTests : IntegrationTest
{
    [Fact]
    public async Task Cascaded_deletes_are_audited_with_their_cause()
    {
        await using MtprApiFactory factory = new();
        factory.Define("author", author);
        factory.Define("book", book("cascade"));
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var authorId = await createAsync(client, "author",
            new
            {
                name = "a"
            }, 
            ct);

        var bookId = await createAsync(client, "book", 
            new
            {
                title = "t", 
                authorId
            }, 
            ct);

        using var deleted =
            await client.DeleteAsync($"/api/author/{authorId}", ct);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rows = (await auditAsync(factory, ct)).Skip(2).ToList();

        rows.Select(r => (r.Entity, r.Operation, r.CausedBy))
            .Should()
            .Equal(
                ("book", "delete", $"author/{authorId}"),
            ("author", "delete", null));

        rows.Should()
            .OnlyContain(r => 
                !string.IsNullOrEmpty(r.Request));

        rows.Select(r => r.Request)
            .Distinct()
            .Should()
            .HaveCount(1);

        bookId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Set_null_changes_are_audited_as_updates()
    {
        await using MtprApiFactory factory = new();
        factory.Define("author", author);
        factory.Define("book", book("setNull"));
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var authorId = await createAsync(client, 
            "author", 
            new
            {
                name = "a"
            },
            ct);

        await createAsync(client, "book", 
            new 
            { 
                title = "t", 
                authorId
            }, 
            ct);

        using var deleted = 
            await client.DeleteAsync($"/api/author/{authorId}", ct);

        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var rows =
            (await auditAsync(factory, ct)).Skip(2).ToList();

        rows.Select(r => 
            (r.Entity, r.Operation, r.CausedBy)).Should().Equal(
            ("book", "update", $"author/{authorId}"), 
            ("author", "delete", null));
    }

    [Fact]
    public async Task Restricted_delete_is_rejected_and_leaves_no_audit_rows()
    {
        await using MtprApiFactory factory = new();
        factory.Define("author", author);
        factory.Define("book", book("restrict"));
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        var authorId =
            await createAsync(client, "author", new { name = "a" }, ct);

        await createAsync(client, "book", 
            new
            {
                title = "t",
                authorId
            }, ct);

        using var deleted = 
            await client.DeleteAsync($"/api/author/{authorId}", ct);

        deleted.StatusCode.Should().Be(HttpStatusCode.Conflict);

        (await auditAsync(factory, ct)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Denied_requests_are_audited()
    {
        await using MtprApiFactory factory = new()
        {
            GrantTestPermissions = false
        };

        factory.Define("author", author);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        using var response =
            await client.PostAsJsonAsync("/api/author", 
                new
                {
                    name = "a"
                }, 
                ct);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var row = 
            (await auditAsync(factory, ct)).Single();

        row.Entity.Should().Be("author");
        row.Operation.Should().Be("denied");
        row.Detail.Should().Be("entity:create");
    }

    [Fact]
    public async Task Audit_rows_cannot_be_changed_or_removed()
    {
        await using MtprApiFactory factory = new();
        factory.Define("author", author);
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;

        await createAsync(client, "author", new { name = "a" }, ct);

        await using SqliteConnection connection =
            new($"Data Source={Path.Combine(factory.Root, "test.db")};Pooling=False");

        await connection.OpenAsync(ct);

        foreach (var sql in new[]
                 {
                     "UPDATE __mtpr_audit SET operation = 'x'",
                     "DELETE FROM __mtpr_audit"
                 })
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;

            var act = () => command.ExecuteNonQueryAsync(ct);

            (await act.Should().ThrowAsync<SqliteException>())
                .WithMessage("*append-only*");
        }
    }
}
