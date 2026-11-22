using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security.Tenancy;

public class TenantRelationTests : IntegrationTest
{
    [Fact]
    public async Task Writes_to_entities_related_to_tenant_data_are_denied()
    {
        await using MtprApiFactory factory = new();
        factory.Define("parent", """
            {
              "name": "parent",
              "fields": [
                { "name": "tenant", "type": "string" }
              ]
            }
            """);
        factory.Define("child", """
            {
              "name": "child",
              "fields": [
                {
                  "name": "parentId",
                  "type": "guid",
                  "references": {
                    "entity": "parent",
                    "onDelete": "cascade"
                  }
                }
              ]
            }
            """);

        factory.ExtraClaims.Add(new Claim("tid", "a"));
        using var client = factory.CreateClient();
        var ct = TestContext.Current.CancellationToken;
        using var parent = await client.PostAsJsonAsync("/api/parent",
            new { tenant = "a" }, ct);
        parent.StatusCode.Should().Be(HttpStatusCode.Created);

        var configuration = factory.Services.GetRequiredService<IConfiguration>();
        configuration["Mtpr:Tenancy:ClaimType"] = "tid";
        configuration["Mtpr:Tenancy:Entities:parent"] = "tenant";
        ((IConfigurationRoot)configuration).Reload();

        using var child = await client.PostAsJsonAsync("/api/child",
            new { parentId = Guid.NewGuid() }, ct);
        child.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using var delete = await client.DeleteAsync($"/api/parent/{Guid.NewGuid()}", ct);
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Invalid_tenant_field_type_is_denied()
    {
        await using MtprApiFactory factory = new();
        factory.Define("note", """
            {
              "name": "note",
              "fields": [
                { "name": "tenant", "type": "int" }
              ]
            }
            """);
        factory.AdditionalConfiguration["Mtpr:Tenancy:Entities:note"] = "tenant";
        factory.ExtraClaims.Add(new Claim("tenant_id", "1"));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/note",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unique_group_without_tenant_field_is_denied()
    {
        await using MtprApiFactory factory = new();
        factory.Define("note", """
            {
              "name": "note",
              "fields": [
                { "name": "tenant", "type": "string" },
                { "name": "title", "type": "string" }
              ],
              "unique": [ [ "title" ] ]
            }
            """);
        factory.AdditionalConfiguration["Mtpr:Tenancy:Entities:note"] = "tenant";
        factory.ExtraClaims.Add(new Claim("tenant_id", "a"));

        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/api/note",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
