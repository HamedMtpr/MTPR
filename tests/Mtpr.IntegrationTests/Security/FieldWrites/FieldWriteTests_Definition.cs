namespace Mtpr.IntegrationTests.Security.FieldWrites;

public partial class FieldWriteTests
{
    private const string definition = """
        {
          "name": "product",
          "fields": [
            {
              "name": "title",
              "type": "string"
            },
            {
              "name": "secret",
              "type": "string"
            }
          ]
        }
        """;
}
