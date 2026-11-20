namespace Mtpr.IntegrationTests.TransactionalRules;

public partial class TransactionalRuleTests
{
    private readonly MtprApiFactory factory = new();

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }

    private const string fields = """
                                  "name": "task", 
                                  "fields": [
                                    { "name": "status", "type": "string", "required": true },
                                    { "name": "priority", "type": "int", "required": true }
                                  ]
                                  """;

    private void define(string trigger) => factory.Define(
        "task", 
        $$"""
              { {{fields}},
                "rules": [{
                "trigger": "{{trigger}}",
                "condition": { "operator": "equal", "left": { "field": "status" }, "right": { "value": "locked" } },
                "action": { "type": "reject", "message": "Rejected" }
              }] }
              """);
}