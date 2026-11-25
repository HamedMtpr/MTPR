using System.Security.Cryptography;

namespace Mtpr.LocalTokenService.SigningKeys;

/// <summary>
/// Uses the first source (in registration order) that yields a key.
/// </summary>
public sealed class ChainedSigningKeyProvider : ISigningKeyProvider
{
    public RSA PrivateKey { get; }
    public RSA PublicKey => PrivateKey;
    public string KeyId { get; }

    public ChainedSigningKeyProvider(IEnumerable<ISigningKeySource> sources)
    {
        PrivateKey = sources
            .Select(source => source.TryGetKey())
            .FirstOrDefault(key => key is not null)
            ?? throw new InvalidOperationException(
                "No signing key source produced a key.");

        KeyId = Convert
            .ToHexString(
                SHA256.HashData(
                    PrivateKey.ExportRSAPublicKey()))[..16]
            .ToLowerInvariant();
    }
}
