using System.Security.Cryptography;

namespace Mtpr.LocalTokenService.SigningKeys;

public interface ISigningKeySource
{
    /// <summary>
    /// Returns the signing key, or null when this source is not configured.
    /// </summary>
    RSA? TryGetKey();
}
