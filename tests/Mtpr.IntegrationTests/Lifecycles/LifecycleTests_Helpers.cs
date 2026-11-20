namespace Mtpr.IntegrationTests.Lifecycles;

public partial class LifecycleTests
{
    private readonly MtprApiFactory factory = new();

    private const string definition = """
                                      { "name": "task", "fields": [
                                        { "name": "status", "type": "string", "required": true },
                                        { "name": "title", "type": "string", "required": true }
                                      ], "lifecycle": { "field": "status", "initial": "draft",
                                        "states": ["draft", "review", "done", "cancelled"],
                                        "transitions": [{ "from": "draft", "to": "review" },
                                                        { "from": "review", "to": "done" },
                                                        { "from": "draft", "to": "cancelled" }] } }
                                      """;

    public LifecycleTests() => factory.Define("task", definition);

    public void Dispose()
    {
        factory.Dispose();
        GC.SuppressFinalize(this);
    }
}