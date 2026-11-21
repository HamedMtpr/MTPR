namespace Mtpr.IntegrationTests.Security.Audits;

public partial class AuditTests
{
    private const string definition = """
                                      { 
                                         "name":"product", 
                                         "fields":[{"name":"title","type":"string","required":true}],
                                         "rules":[
                                         {
                                            "trigger":"afterUpdate",
                                            "condition":
                                            {
                                               "operator":"equal",
                                               "left":{"field":"title"},
                                               "right":{"value":"blocked"}
                                            },
                                            "action":{"type":"reject","message":"Rejected"}
                                         }] 
                                      }
                                      """;
}