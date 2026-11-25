using System.Security.Cryptography;

namespace Mtpr.LocalTokenService;

public interface ISigningKeyProvider
{
    RSA PrivateKey { get; }
    RSA PublicKey { get; }
    string KeyId { get; }
}