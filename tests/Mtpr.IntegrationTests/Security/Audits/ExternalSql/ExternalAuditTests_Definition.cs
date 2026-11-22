namespace Mtpr.IntegrationTests.Security.Audits.ExternalSql;

public partial class ExternalAuditTests
{
    private const string definition = """
        {
          "name": "product",
          "fields": [
            { "name": "title", "type": "string" }
          ]
        }
        """;

    private const string child = """
        {
          "name": "child",
          "fields": [
            {
              "name": "parentId",
              "type": "guid",
              "references": { "entity": "product", "onDelete": "cascade" }
            }
          ]
        }
        """;

    private const string uniqueProduct = """
        {
          "name": "product",
          "fields": [
            { "name": "title", "type": "string" }
          ],
          "unique": [["title"]]
        }
        """;
}
