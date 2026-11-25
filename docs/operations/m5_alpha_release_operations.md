# m5_alpha_release: operations

- [Back to index](index.md)
- [M5 build and run](../building/m5_alpha_release_build.md)
- [M5 implementation](../coding/m5_alpha_release_implementation.md)
- [M5 deployment](../deployment/m5_alpha_release_deployment.md)
- [M5 testing](../testing/m5_alpha_release_testing.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)
- [M4 operations](m4_security_multi_tenancy_operations.md)
- [Token service settings](../../tools/Mtpr.LocalTokenService/appsettings.json)
- [Helpdesk sample definitions](../examples/m5-helpdesk/README.md)

## Token service clients and roles

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

## Sample application

The API watches the definitions folder, so an edited helpdesk JSON file applies to the next request without a restart. In the container the definitions are in `/app/definitions` and the SQLite database is in the `/data` volume; back up that volume to keep the data.
