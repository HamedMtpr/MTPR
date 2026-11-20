using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace Mtpr.IntegrationTests;

/// <summary>Hosts the API against a temp definitions directory and a temp SQLite file.</summary>
public sealed class MtprApiFactory : WebApplicationFactory<Program>
{
    public bool UseTestAuthentication { get; set; } = true;
    public bool GrantTestPermissions { get; set; } = true;
    public Dictionary<string, string?> AdditionalConfiguration { get; } = [];
    public Action<JwtBearerOptions>? ConfigureJwtBearer { get; set; }
    public string Root { get; } = Directory.CreateTempSubdirectory("mtpr-").FullName;

    public string DefinitionsPath => Path.Combine(Root, "definitions");

    public MtprApiFactory() => Directory.CreateDirectory(DefinitionsPath);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.ConfigureAppConfiguration((_, config) =>
        {
            var settings = new Dictionary<string, string?>
                {
                    ["Mtpr:Authentication:Authority"] =
                        "https://identity.example.invalid",
                    ["Mtpr:Authentication:Audience"] = "mtpr-tests",
                    ["Mtpr:DefinitionsPath"] = DefinitionsPath,
                    ["Mtpr:ConnectionString"] =
                        $"Data Source={Path.Combine(Root, "test.db")};Pooling=False"
                };
            if (GrantTestPermissions)
                foreach (var (action, index) in new[] { "read", "create", "update", "delete" }.Select((action, index) => (action, index)))
                    settings[$"Mtpr:Authorization:Roles:test-role:*:{index}"] = action;
            foreach (var pair in AdditionalConfiguration)
                settings[pair.Key] = pair.Value;
            config.AddInMemoryCollection(settings);
        })
            .ConfigureServices(services =>
            {
                if (ConfigureJwtBearer is not null)
                    services.Configure(JwtBearerDefaults.AuthenticationScheme, ConfigureJwtBearer);
                if (UseTestAuthentication)
                    services.AddAuthentication(options =>
                        {
                            options.DefaultAuthenticateScheme = "Test";
                            options.DefaultChallengeScheme = "Test";
                        })
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", 
                            _ => { });
            });

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

public sealed class TestAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : 
    AuthenticationHandler<AuthenticationSchemeOptions>(options, 
        logger, 
        encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, "test-user"), new Claim(ClaimTypes.Role, "test-role")],
            Scheme.Name);
        
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(identity), 
                Scheme.Name)));
    }
}
