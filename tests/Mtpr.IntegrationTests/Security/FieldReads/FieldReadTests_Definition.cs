namespace Mtpr.IntegrationTests.Security.FieldReads;

public partial class FieldReadTests
{
    private const string definition =
        """
        {
          "name": "product",
          "fields": [
            { "name": "title",  "type": "string" },
            { "name": "secret", "type": "string" }
          ]
        }
        """;
}