using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Mtpr.IntegrationTests;

/// <summary>Hosts the API against a temp definitions directory and a temp SQLite file.</summary>
public sealed class MtprApiFactory : WebApplicationFactory<Program>
{
    public string Root { get; } = Directory.CreateTempSubdirectory("mtpr-").FullName;

    public string DefinitionsPath => Path.Combine(Root, "definitions");

    public MtprApiFactory() => Directory.CreateDirectory(DefinitionsPath);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureAppConfiguration((_, config) => 
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Mtpr:DefinitionsPath"] = DefinitionsPath,
                    ["Mtpr:ConnectionString"] =
                        $"Data Source={Path.Combine(Root, "test.db")};Pooling=False"
                }));

    public void Define(string name, string json) => 
        File.WriteAllText(Path.Combine(DefinitionsPath, name + ".json"), 
            json);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing && Directory.Exists(Root))
            Directory.Delete(Root, true);
    }
}
