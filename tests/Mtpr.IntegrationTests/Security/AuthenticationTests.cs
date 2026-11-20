using System.Net;
using System.Net.Http.Headers;
using Mtpr.TestUtilities;

namespace Mtpr.IntegrationTests.Security;

public class AuthenticationTests : IntegrationTest
{
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task Crud_rejects_anonymous_requests(string method)
    {
        await using var factory = new MtprApiFactory
        {
            UseTestAuthentication = false
        };

        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method),
            $"/api/product/{(method is "PUT" or
                "PATCH" or 
                "DELETE" 
                ? Guid.NewGuid().ToString() 
                : string.Empty)}");

        using var response = 
            await client.SendAsync(request, 
                TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Invalid_bearer_token_is_rejected()
    {
        await using var factory = new MtprApiFactory
        {
            UseTestAuthentication = false
        };

        using var client = factory.CreateClient();

        client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer",
                "invalid-token");

        using var response = await client.GetAsync("/api/product",
            TestContext.Current.CancellationToken);

        response.StatusCode
            .Should()
            .Be(HttpStatusCode.Unauthorized);
    }
}
