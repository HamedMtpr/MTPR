# M5 local token service: testing

- [Back to index](index.md)
- [M5 build and run](../building/m5_local_token_service_build.md)
- [M5 implementation](../coding/m5_local_token_service_implementation.md)
- [M5 deployment](../deployment/m5_local_token_service_deployment.md)
- [M5 operations](../operations/m5_local_token_service_operations.md)
- [M4 security and multi-tenancy build](../building/m4_security_multi_tenancy_build.md)

- [Token issuer](../../tools/Mtpr.LocalTokenService/JwtTokenIssuer.cs)
- [JWT validation tests](../../tests/Mtpr.IntegrationTests/Security/JwtValidations/JwtValidationTests.cs)

The token service has no automated test project yet. The M4 integration tests validate the API against a simulated identity provider and remain the automated coverage for token validation. The service itself is verified manually.

## Build and regression check

```powershell
dotnet build MTPR.slnx -warnaserror
dotnet test MTPR.slnx
```

## Manual checks

| Check | Expected result |
| --- | --- |
| `POST /token` with valid client credentials | HTTP 200 with a Bearer token and `expires_in` of `TokenLifetimeMinutes * 60`. |
| Wrong secret or unknown client | HTTP 401, `invalid_client`. |
| Another `grant_type` | HTTP 400, `unsupported_grant_type`. |
| `GET /.well-known/jwks.json` | One RS256 key whose `kid` equals the token header `kid`. |
| Restart with the same key file | Same `kid`. |
| Restart with `LocalTokenService__SigningKeyPem` set | Different `kid` from the file key. |
| Invalid PEM in `SigningKeyPem` | Startup or first-use failure; the file key is not used. |
| API call with the issued token (matching Authority and Audience) | HTTP 200 for a permitted role; HTTP 403 without a matching grant. |

Last full run on the solution: 279 unit, 9 test-utility and 136 integration tests passed.
