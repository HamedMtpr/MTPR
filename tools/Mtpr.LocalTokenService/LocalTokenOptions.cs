namespace Mtpr.LocalTokenService;

public sealed class LocalTokenOptions
{
    public const string SectionName = "LocalTokenService";

    public string Issuer { get; set; } = "https://localhost:5443";
    public string Audience { get; set; } = "mtpr-api";
    public string? SigningKeyPem { get; set; }
    public string SigningKeyPath { get; set; } = "keys/signing-key.pem";
    public int TokenLifetimeMinutes { get; set; } = 60;
    public Dictionary<string, LocalClient> Clients { get; set; } = new();
}