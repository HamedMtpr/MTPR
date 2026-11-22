using System.Net;
using System.Text;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security;

public class EntityAuthorizationTests : IntegrationTest
{
    [Theory]
    [InlineData("POST", "create", HttpStatusCode.Created)]
    [InlineData("PUT", "update", HttpStatusCode.NotFound)]
    [InlineData("PATCH", "update", HttpStatusCode.NotFound)]
    [InlineData("DELETE", "delete", HttpStatusCode.NotFound)]
    public async Task Matching_operation_grant_allows_request_to_reach_engine(string method, string action, HttpStatusCode expected)
    {
        await using MtprApiFactory factory = new()
        {
            GrantTestPermissions = false
        };

        factory
            .AdditionalConfiguration[$"Mtpr:Authorization:Roles:test-role:product:0"] = action;

        factory.Define("product", 
            """{"name":"product","fields":[{"name":"title","type":"string"}]}""");

        using var client = factory.CreateClient();

        var path = method == "POST" 
            ? "/api/product" 
            : "/api/product/00000000-0000-0000-0000-000000000001";

        using HttpRequestMessage request = new(new HttpMethod(method), path);

        if (method is "POST" or "PUT" or "PATCH")
            request.Content = new StringContent("{\"title\":\"Pen\"}",
                Encoding.UTF8,
                "application/json");

        using var response = 
            await client.SendAsync(request,
                TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);
    }

    [Theory]
    [InlineData("GET", "/api/product", HttpStatusCode.OK)]
    [InlineData("GET", "/api/other", HttpStatusCode.Forbidden)]
    [InlineData("POST", "/api/product", HttpStatusCode.Forbidden)]
    [InlineData("PUT", "/api/product/00000000-0000-0000-0000-000000000001", HttpStatusCode.Forbidden)]
    [InlineData("PATCH", "/api/product/00000000-0000-0000-0000-000000000001", HttpStatusCode.Forbidden)]
    [InlineData("DELETE", "/api/product/00000000-0000-0000-0000-000000000001", HttpStatusCode.Forbidden)]
    public async Task Grants_apply_only_to_configured_entity_and_operation(string method, string path, HttpStatusCode expected)
    {
        await using MtprApiFactory factory = new()
        {
            GrantTestPermissions = false
        };

        factory
            .AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:product:0"] = "read";

        factory.Define("product",
            """{"name":"product","fields":[{"name":"title","type":"string"}]}""");

        using var client = factory.CreateClient();
        using HttpRequestMessage request = new(new HttpMethod(method), path);

        if (method is "POST" or "PUT" or "PATCH")
            request.Content = new StringContent("{}", 
                Encoding.UTF8, 
                "application/json");

        using var response = await client.SendAsync(request, 
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task Authenticated_user_without_a_grant_is_denied()
    {
        await using MtprApiFactory factory = new()
        {
            GrantTestPermissions = false
        };
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/product",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Grant_for_another_role_does_not_allow_access()
    {
        await using MtprApiFactory factory = new()
        {
            GrantTestPermissions = false
        };

        factory
            .AdditionalConfiguration["Mtpr:Authorization:Roles:other-role:product:0"] = "read";

        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/product",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unknown_permission_fails_startup()
    {
        await using MtprApiFactory factory = new()
        {
            GrantTestPermissions = false
        };

        factory
            .AdditionalConfiguration["Mtpr:Authorization:Roles:test-role:product:0"] = "reed";

        var start = () => factory.CreateClient();

        start.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Invalid permissions*");
    }
}
