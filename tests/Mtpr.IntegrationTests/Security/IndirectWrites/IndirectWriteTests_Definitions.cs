namespace Mtpr.IntegrationTests.Security.IndirectWrites;

public partial class IndirectWriteTests
{
    private const string parent = """
        {
          "name": "parent",
          "fields": [
            { "name": "name", "type": "string" }
          ]
        }
        """;

    private static string child(string behavior) => $$"""
        {
          "name": "child",
          "fields": [
            {
              "name": "parentId",
              "type": "guid",
              "references": { "entity": "parent", "onDelete": "{{behavior}}" }
            }
          ]
        }
        """;

    private const string line = """
        {
          "name": "line",
          "fields": [
            { "name": "price", "type": "decimal" },
            { "name": "quantity", "type": "int", "default": 1 },
            {
              "name": "total",
              "type": "decimal",
              "computed": {
                "operator": "multiply",
                "left": { "field": "price" },
                "right": { "field": "quantity" }
              }
            }
          ]
        }
        """;
}
