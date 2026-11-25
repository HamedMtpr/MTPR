# m5_alpha_release: deployment

- [Back to index](index.md)
- [M5 build and run](../building/m5_alpha_release_build.md)
- [M5 implementation](../coding/m5_alpha_release_implementation.md)
- [M5 operations](../operations/m5_alpha_release_operations.md)
- [M5 testing](../testing/m5_alpha_release_testing.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)
- [Token service publish profile](../../tools/Mtpr.LocalTokenService/Properties/PublishProfiles/FolderProfile.pubxml)
- [Token service settings](../../tools/Mtpr.LocalTokenService/appsettings.json)
- [Dockerfile](../../Dockerfile)
- [Docker ignore file](../../.dockerignore)

The token service is for development and test environments. Do not expose it to untrusted networks or use it as the identity provider of a production deployment; the API there must trust a real OIDC provider.

## Publish the token service

```powershell
dotnet publish tools/Mtpr.LocalTokenService -p:PublishProfile=FolderProfile
```

The profile publishes a self-contained, single-file, ReadyToRun build for `win-x64` on `net10.0`. Other runtimes need a different profile or `-r` option.

## Configure the token service

- Set `LocalTokenService__Issuer` to the HTTPS URL the API will use as its authority.
- Replace the placeholder client secrets (`reader-secret-change-me`, `editor-secret-change-me`) with generated values supplied by environment variables or User Secrets, not by committed files.
- Provide the signing key with `LocalTokenService__SigningKeyPem`, or let the service create `keys/signing-key.pem` beside the application on first start.
- Never commit a private key. The repository ignores `tools/**/keys/` and `*.pem`.
- Serve the service over HTTPS so the API can fetch discovery and keys.

Set the API `Mtpr:Authentication:Authority` and `Audience` to the issuer and audience of the service. The API reads signing keys over the network, so the service must be running before the API validates its first token.

## Run the sample application container

The Dockerfile builds `src/Mtpr.Api` and bundles the helpdesk definitions in `/app/definitions`. The SQLite database lives in the `/data` volume. The API listens on port 8080 inside the container and is exposed on host port 5080; put TLS termination in front of it.

```powershell
docker build -t mtpr-helpdesk .
docker run -p 5080:8080 -v mtpr-data:/data `
    -e Mtpr__Authentication__Authority=https://identity.example.com `
    -e Mtpr__Authentication__Audience=mtpr-api `
    -e Mtpr__Authorization__Roles__editor__ticket__0=read `
    mtpr-helpdesk
```

Authority must be an absolute HTTPS URL of a real OIDC provider. To change definitions without rebuilding or restarting, mount a folder over `/app/definitions` with `-v ${PWD}/definitions:/app/definitions`; file changes are picked up while the container runs. Do not use the local token service in production.
