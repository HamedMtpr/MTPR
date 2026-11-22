using System.Security.Claims;

namespace Mtpr.IntegrationTests.Security.Tenancy;

internal class TestDoublesTenancy
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

    public static string EntityKey => "Mtpr:Tenancy:Entities:note";

    public static MtprApiFactory CreateFactory()
    {
        MtprApiFactory factory = new();
        factory.Define("note", Definition);
        factory.AdditionalConfiguration[EntityKey] = "tenant";
        factory.AdditionalConfiguration["Mtpr:Tenancy:ClaimType"] = "tid";
        factory.ExtraClaims.Add(new Claim("tid", "a"));

        return factory;
    }

    public static void SwitchTo(MtprApiFactory factory, string? tenant)
    {
        factory.ExtraClaims.Clear();

        if (tenant is not null)
            factory.ExtraClaims.Add(new Claim("tid", tenant));
    }
}
