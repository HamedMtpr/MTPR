using System.Text.Json;
using Json.Schema;
using Mtpr.TestUtilities;

namespace Mtpr.Tests.M0FoundationHardeningSchema;

public class M0FoundationHardeningSchemaTests : UnitTest
{
    private static readonly JsonSchema Schema = JsonSchema.FromText(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "M0FoundationHardeningSchema", "m0_foundation_hardening.schema.json")));

    [Fact]
    public void Create_rule_definition_is_valid()
    {
        const string json =
            """
            { 
               "name": "task", 
               "fields": [{ "name": "priority", "type": "int" }],
              "rules": [
              { 
                 "trigger": "beforeCreate",
                 "condition": 
                 { 
                    "operator": "greaterThan", 
                    "left": { "field": "priority" },
                     "right": { "value": 5 } 
                 },
                "action": 
                { 
                   "type": "reject", 
                   "message": "Too high" 
                } 
              }] 
            }
            """;
        using var document = JsonDocument.Parse(json);
        Schema.Evaluate(document.RootElement).IsValid.Should().BeTrue();
    }
    [Fact]
    public void Lifecycle_definition_is_valid()
    {
        const string json = """
            { "name": "task", "fields": [{ "name": "status", "type": "string", "required": true }],
              "lifecycle": { "field": "status", "initial": "draft", "states": ["draft", "done"],
              "transitions": [{ "from": "draft", "to": "done" }] } }
            """;
        using var document = JsonDocument.Parse(json);
        Schema.Evaluate(document.RootElement)
            .IsValid
            .Should()
            .BeTrue();
    }
    [Theory]
    [InlineData("author.json")]
    [InlineData("book.json")]
    [InlineData("booktag.json")]
    [InlineData("tag.json")]
    public void Library_definitions_are_valid(string fileName)
    {
        var json = File.ReadAllText(
            Path.Combine(
                AppContext.BaseDirectory, 
                "m2-library", 
                fileName));
        using var document = JsonDocument.Parse(json);

        Schema.Evaluate(document.RootElement).IsValid
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData("missing entity name", "{\"fields\":[{\"name\":\"title\",\"type\":\"string\"}]}")]
    [InlineData("empty fields", "{\"name\":\"book\",\"fields\":[]}")]
    [InlineData("reserved field name", "{\"name\":\"book\",\"fields\":[{\"name\":\"ID\",\"type\":\"guid\"}]}")]
    [InlineData("unknown field type", "{\"name\":\"book\",\"fields\":[{\"name\":\"title\",\"type\":\"text\"}]}")]
    [InlineData("reference on non-guid", "{\"name\":\"book\",\"fields\":[{\"name\":\"author\",\"type\":\"string\",\"references\":{\"entity\":\"author\"}}]}")]
    [InlineData("length on numeric field", "{\"name\":\"book\",\"fields\":[{\"name\":\"price\",\"type\":\"decimal\",\"minLength\":1}]}")]
    [InlineData("numeric constraint on string", "{\"name\":\"book\",\"fields\":[{\"name\":\"title\",\"type\":\"string\",\"min\":1}]}")]
    [InlineData("invalid delete behavior", "{\"name\":\"book\",\"fields\":[{\"name\":\"authorId\",\"type\":\"guid\",\"references\":{\"entity\":\"author\",\"onDelete\":\"erase\"}}]}")]
    [InlineData("unknown property", "{\"name\":\"book\",\"fields\":[{\"name\":\"title\",\"type\":\"string\"}],\"unexpected\":true}")]
    [InlineData("duplicate unique fields", "{\"name\":\"book\",\"fields\":[{\"name\":\"title\",\"type\":\"string\"}],\"unique\":[[\"title\",\"title\"]]}")]
    public void Invalid_definitions_are_rejected(string reason, string json)
    {
        using var document = JsonDocument.Parse(json);
        Schema.Evaluate(document.RootElement).IsValid
            .Should()
            .BeFalse(reason);
    }
}
