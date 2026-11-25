# m5_alpha_release: implementation

- [Back to index](index.md)
- [M5 build and run](../building/m5_alpha_release_build.md)
- [M5 deployment](../deployment/m5_alpha_release_deployment.md)
- [M5 operations](../operations/m5_alpha_release_operations.md)
- [M5 testing](../testing/m5_alpha_release_testing.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)
- [Program](../../tools/Mtpr.LocalTokenService/Program.cs)
- [Token issuer](../../tools/Mtpr.LocalTokenService/JwtTokenIssuer.cs)
- [Options](../../tools/Mtpr.LocalTokenService/LocalTokenOptions.cs)
- [Chained key provider](../../tools/Mtpr.LocalTokenService/SigningKeys/ChainedSigningKeyProvider.cs)
- [Configuration key source](../../tools/Mtpr.LocalTokenService/SigningKeys/ConfigurationSigningKeySource.cs)
- [File key source](../../tools/Mtpr.LocalTokenService/SigningKeys/FileSigningKeySource.cs)
- [Helpdesk sample definitions](../examples/m5-helpdesk/README.md)
- [Sample integration tests](../../tests/Mtpr.IntegrationTests/HelpdeskSample/HelpdeskSampleTests.cs)

## Token service structure

The service is a minimal ASP.NET Core application. `Program.cs` only wires dependency injection and maps the three endpoints; token creation and key handling are behind interfaces so each class has one responsibility.

| Type | Responsibility |
| --- | --- |
| `ITokenIssuer` / `JwtTokenIssuer` | Builds the claims (`sub`, one `role` claim per role, optional `tenant_id`) and signs with RS256. Issuer, audience and lifetime come from options. |
| `ISigningKeyProvider` | Exposes the private key, public key and `kid`. |
| `ISigningKeySource` | Yields a key or null when not configured. |
| `ConfigurationSigningKeySource` | Reads `LocalTokenService:SigningKeyPem` (User Secrets or environment variable). Literal `\n` sequences are accepted for single-line values. |
| `FileSigningKeySource` | Reads `SigningKeyPath` (default `keys/signing-key.pem`); creates a 2048-bit key with owner-only permissions on Unix when missing. |
| `ChainedSigningKeyProvider` | Uses the first source that yields a key. The `kid` is the first 16 hex characters of the SHA-256 hash of the public key. |

New key origins (for example a vault) are added by registering another `ISigningKeySource`; no existing class changes. Registration order in `Program.cs` is precedence: configuration first, then file.

## Token service behavior

- `/token` accepts only `grant_type=client_credentials`; other values return `unsupported_grant_type` (HTTP 400). Unknown client or wrong secret returns `invalid_client` (HTTP 401). Secrets are compared in constant time.
- Options are read through `IOptionsMonitor`, so changes to clients, issuer, audience and lifetime from a reloadable configuration provider apply without a restart. The signing key is loaded once at startup; changing it requires a restart.
- An invalid PEM in `SigningKeyPem` fails with an error rather than falling back to the file, so a misconfigured key is never silently replaced.
- `tenant_id` matches the claim type read by the API tenant policy, so tokens can drive the M4 tenant isolation.

## Sample application

The helpdesk sample adds no production code. It shows the target domain can be expressed only with entity definitions.

| Entity | Features used |
| --- | --- |
| `customer` | Required string constraints, unique `email`. |
| `ticket` | Reference to `customer` (restrict), literal defaults, computed `cost` (`hours` × `hourlyRate`), lifecycle `open → inProgress → resolved → closed` (with reopening), `beforeCreate` rule rejecting more than 40 hours. |
| `ticketcomment` | Reference to `ticket` with cascade delete. |

The integration test project copies the JSON files to its output directory (`m5-helpdesk`) and loads them through `MtprApiFactory.Define`, so the documented files are the tested files. Editing a file changes behavior without a restart.
