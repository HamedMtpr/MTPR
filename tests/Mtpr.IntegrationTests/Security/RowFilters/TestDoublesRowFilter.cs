using System.Security.Claims;

namespace Mtpr.IntegrationTests.Security.RowFilters;

internal class TestDoublesRowFilter
{
    public static string Definition =>
        """
        {
          "name": "note",
          "fields": [
            { "name": "title", "type": "string" },
            { "name": "tenant", "type": "string" }
          ]
        }
        """;

    public static string Key =>
        "Mtpr:Authorization:RowFilters:note:test-role:0";

    public static MtprApiFactory CreateFactory()
    {
        MtprApiFactory factory = new();
        factory.Define("note", Definition);
        factory.ExtraClaims.Add(new Claim("tenant", "a"));
        factory.AdditionalConfiguration[Key] =
            "tenant:eq:{claim:tenant}";

        return factory;
    }
}
