using System.Net;
using System.Text.Json;

namespace Mtpr.IntegrationTests.Security.JwtValidations;

public partial class JwtValidationTests
{
    private sealed class TestDiscoveryHandler(string jsonWebKeySet) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = request.RequestUri?.AbsolutePath switch
            {
                "/.well-known/openid-configuration" => JsonSerializer.Serialize(new
                {
                    issuer = issuer,
                    jwks_uri = issuer + "/keys"
                }),
                "/keys" => jsonWebKeySet,
                _ => null
            };

            return Task.FromResult(content is null
                ? new HttpResponseMessage(HttpStatusCode.NotFound)
                : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content =
                        new StringContent(content,
                            System.Text.Encoding.UTF8,
                            "application/json")
                });
        }
    }
}