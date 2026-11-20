namespace Mtpr.IntegrationTests.ComputedValues;

public partial class ComputedValueTests
{
    private readonly MtprApiFactory factory = new();

    private const string definition = 
        """
        { 
           "name": "line", 
           "fields": [
           { 
              "name": "price", 
              "type": "decimal", 
              "required": true 
           },
           { 
              "name": "quantity",
               "type": "int",
                "required": true,
                 "default": 1
           },
           { 
              "name": "total", 
              "type": "decimal",
              "required": true, 
              "max": 100,
              "computed": 
              { 
                 "operator": "multiply", 
                 "left": { "field": "price" }, 
                 "right": { "field": "quantity" } 
              } 
           }],
           "rules": [
           { 
              "trigger": "beforeUpdate",
              "condition": 
              { 
                 "operator": "greaterThan",
                 "left": { "field": "total" }, 
                 "right": { "value": 50 } 
              },
           "action":
           { 
              "type": "reject",
              "message": "total too high" } 
           }] 
        }
        """;

    public ComputedValueTests() => 
        factory.Define("line", definition);

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}

