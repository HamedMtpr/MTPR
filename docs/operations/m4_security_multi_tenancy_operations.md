# M4 security and multi-tenancy: operations

- [Back to index](index.md)
- [M4 deployment](../deployment/m4_security_multi_tenancy_deployment.md)
- [M4 build and run](../building/m4_security_multi_tenancy_build.md)
- [M4 implementation](../coding/m4_security_multi_tenancy_implementation.md)
- [M4 testing](../testing/m4_security_multi_tenancy_testing.md)
- [M3 operations](m3_business_rules_operations.md)

## Configure role grants

Configure grants under `Mtpr:Authorization:Roles` in application configuration. For example:

```json
{
  "Mtpr": {
    "Authorization": {
      "Roles": {
        "reader": { "product": ["read"] },
        "editor": { "product": ["read", "create", "update", "delete"] }
      }
    }
  }
}
```

`read` permits both list and single-record GET; `update` permits PUT and PATCH. The entity name `*` grants the listed operations on all entities. Role names must match role claims on the validated principal; entity names are case-insensitive and operation names are lowercase and case-sensitive. Grants from multiple roles combine, and absence of a matching grant denies access. The API loads grants at startup, so restart after a change and verify allowed and denied cases. Configure authority and audience for the intended trusted issuer and API. Granting a wildcard also grants access to future entities.

## Diagnose access

| Result | What to inspect |
| --- | --- |
| Startup failure | HTTPS authority, nonempty audience, and spelling of configured operations. |
| HTTP 401 | Bearer token presence, signature, issuer, audience and lifetime; issuer discovery and signing-key availability. |
| HTTP 403 | Principal role claims, role spelling and matching entity and operation grants; restart after changing grants. |
| Unexpected data access | Broad role grants, wildcard entity grants, direct database or engine access, and cascade behavior. |

Do not log or paste bearer tokens when troubleshooting. Identity-provider settings and role claims should be checked against the actual deployment. If permissions are changed or removed at the provider, account for the lifetime of already issued tokens.

## Data protection and recovery

The API does not implement field permissions, row filters or tenant isolation. An entity-level grant covers all records for that operation; mutation responses can include fields without a separate read grant. SQLite cascades and set-null changes do not check grants on related entities. Restrict direct database and engine access because endpoint authorization does not apply there.

Successful direct writes add snapshots to `__mtpr_audit` in the same transaction. The table contains potentially sensitive values and is protected only by the database's access controls. It has no built-in retention, query API, independent storage or tamper detection. Cascades, direct SQL, reads and failed or denied requests are not logged. Subject can be null and is stored without issuer. Plan storage capacity and backups for growth; if audit insertion fails, the corresponding write rolls back. Do not treat this partial log as a complete audit trail.

Back up the database and matching definitions together, along with a separately protected copy of the deployment's authentication and authorization configuration. Stop the API and other writers before restoring. Verify authentication, allowed and denied operations, and representative data in an isolated copy before resuming service. Refer to the M3 operations guide for definition reloads and SQLite recovery details.
