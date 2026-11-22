using Microsoft.Extensions.Primitives;

namespace Mtpr.Api.Authorization;

/// <summary>
/// Holds an authorization policy built from configuration and rebuilds it
/// whenever the configuration reloads, so changes apply without a restart.
/// The first build must succeed. If a later build fails, the previous valid
/// policy stays active and the error is logged, so a bad edit never opens
/// or closes access by accident.
/// </summary>
public sealed class ReloadablePolicy<T> : IDisposable
    where T : class
{
    private readonly IConfiguration configuration;
    private readonly Func<IConfiguration, T> factory;
    private readonly ILogger logger;
    private readonly string name;
    private readonly IDisposable registration;
    private readonly object gate = new();

    private volatile T current;

    public ReloadablePolicy(IConfiguration configuration,
        Func<IConfiguration, T> factory,
        ILogger logger,
        string name)
    {
        this.configuration = configuration;
        this.factory = factory;
        this.logger = logger;
        this.name = name;

        current = factory(configuration);

        registration = ChangeToken.OnChange(configuration.GetReloadToken,
            reload);
    }

    public T Current => current;

    public void Dispose() => registration.Dispose();

    private void reload()
    {
        lock (gate)
        {
            try
            {
                current = factory(configuration);

                logger.LogInformation(
                    "Reloaded {Policy} policy.",
                    name);
            }
            catch (Exception exception)
            {
                logger.LogError(exception,
                    "Invalid {Policy} policy ignored; the previous policy stays active.",
                    name);
            }
        }
    }
}
