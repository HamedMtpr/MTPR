namespace Mtpr.IntegrationTests.CreateRules;

public partial class CreateRuleTests
{
    private const string definition = 
        """
        { 
           "name": "task",
           "fields": [
           { 
              "name": "priority", 
              "type": "int", 
              "required": true, 
              "default": 2 
           },
           { 
              "name": "status", 
              "type": "string" 
           }], 
           "rules": [
           {
              "trigger": "beforeCreate",
              "condition": 
              {
                 "operator": "and",
                 "conditions": [
                 { 
                    "operator": "greaterThan",
                     "left": 
                     { 
                        "field": "priority" 
                     },
                     "right":
                     { 
                        "value": 5 
                     } 
                 },
                 { 
                    "operator": "equal", 
                    "left":{ "field": "status" }, 
                    "right": { "value": "open" } 
                 }] 
              },
              "action": 
              { 
                 "type": "reject",
                 "message": "Priority is too high" 
              }
           }] 
        }
        """;
}