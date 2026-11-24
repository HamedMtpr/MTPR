# M5 local token service: operations

- [Back to index](index.md)
- [M5 build and run](../building/m5_local_token_service_build.md)
- [M5 implementation](../coding/m5_local_token_service_implementation.md)
- [M5 deployment](../deployment/m5_local_token_service_deployment.md)
- [M5 testing](../testing/m5_local_token_service_testing.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)

- [Token service settings](../../tools/Mtpr.LocalTokenService/appsettings.json)
- [M4 operations](m4_security_multi_tenancy_operations.md)

## Clients and roles

Clients live under `LocalTokenService:Clients`, each with `Secret`, `Subject`, `Roles` and optional `TenantId`. Roles become `role` claims, which must match the grants under `Mtpr:Authorization:Roles` in the API. `TenantId` becomes the `tenant_id` claim used by tenant isolation. Client and lifetime changes from a reloadable configuration provider apply without a restart; environment variable changes need one. Tokens already issued stay valid until they expire.

## Signing key

| Situation | Action |
| --- | --- |
| Key file created automatically | Back it up if tokens must stay valid across machines; keep it out of source control. |
| Rotate the key | Delete `keys/signing-key.pem` or change `SigningKeyPem`, restart the service. Tokens signed by the old key are rejected once the API refreshes its keys. |
| Key leaked | Rotate immediately and replace client secrets. If it was ever committed, treat it as compromised. |

The `kid` changes with the key, so the API can tell old and new keys apart.

## Diagnose

| Symptom | Check |
| --- | --- |
| API returns 401 | Authority equals `LocalTokenService:Issuer`; audience matches; token not expired; discovery and JWKS reachable over HTTPS and the development certificate is trusted. |
| API returns 403 | Client roles match the API role grants; tenant claim present when tenancy is enabled. |
| Startup error about the PEM | `SigningKeyPem` is not a valid PEM; correct it or remove it to use the file. |
| `invalid_client` | Client id and secret; secrets are case sensitive. |

Do not log or share issued tokens, client secrets or the PEM.
