using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace Mtpr.LocalTokenService.SigningKeys;

/// <summary>
/// Reads the PEM from configuration (User Secrets or environment variable
/// LocalTokenService__SigningKeyPem). Literal "\n" sequences are accepted
/// so the PEM can be written on a single line.
/// </summary>
public sealed class ConfigurationSigningKeySource(
    IOptions<LocalTokenOptions> options) : ISigningKeySource
{
    public RSA? TryGetKey()
    {
        var pem = options.Value.SigningKeyPem;
        if (string.IsNullOrWhiteSpace(pem))
            return null;

        var rsa = RSA.Create();
        try
        {
            rsa.ImportFromPem(pem.Replace("\\n", "\n"));
            return rsa;
        }
        catch (ArgumentException exception)
        {
            rsa.Dispose();
            throw new InvalidOperationException(
                $"'{LocalTokenOptions.SectionName}:SigningKeyPem' " +
                "is set but is not a valid PEM key.",
                exception);
        }
    }
}
