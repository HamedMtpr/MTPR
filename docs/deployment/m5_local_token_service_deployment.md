# M5 local token service: deployment

- [Back to index](index.md)
- [M5 build and run](../building/m5_local_token_service_build.md)
- [M5 implementation](../coding/m5_local_token_service_implementation.md)
- [M5 operations](../operations/m5_local_token_service_operations.md)
- [M5 testing](../testing/m5_local_token_service_testing.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)

- [Publish profile](../../tools/Mtpr.LocalTokenService/Properties/PublishProfiles/FolderProfile.pubxml)
- [Token service settings](../../tools/Mtpr.LocalTokenService/appsettings.json)

The token service is for development and test environments. Do not expose it to untrusted networks or use it as the identity provider of a production deployment; the API there must trust a real OIDC provider.

## Publish

```powershell
dotnet publish tools/Mtpr.LocalTokenService -p:PublishProfile=FolderProfile
```

The profile publishes a self-contained, single-file, ReadyToRun build for `win-x64` on `net10.0`. Other runtimes need a different profile or `-r` option.

## Configure

- Set `LocalTokenService__Issuer` to the HTTPS URL the API will use as its authority.
- Replace the placeholder client secrets (`reader-secret-change-me`, `editor-secret-change-me`) with generated values supplied by environment variables or User Secrets, not by committed files.
- Provide the signing key with `LocalTokenService__SigningKeyPem`, or let the service create `keys/signing-key.pem` beside the application on first start.
- Never commit a private key. The repository ignores `tools/**/keys/` and `*.pem`.
- Serve the service over HTTPS so the API can fetch discovery and keys.

Set the API `Mtpr:Authentication:Authority` and `Audience` to the issuer and audience of the service. The API reads signing keys over the network, so the service must be running before the API validates its first token.
