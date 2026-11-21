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

## Configure field write grants

Optionally restrict which input fields a role may supply under `Mtpr:Authorization:FieldWrites`, by entity and role:

```json
{
  "Mtpr": {
    "Authorization": {
      "FieldWrites": {
        "product": {
          "editor": ["title", "price"],
          "admin": ["*"]
        }
      }
    }
  }
}
```

The entity operation grant (`create` or `update`) is still required. For a configured entity, POST and PATCH are rejected with HTTP 403 if any supplied field is not granted or the body is not a JSON object; PUT requires `*`. Roles without a field grant cannot write to that entity. Entities without a policy keep their entity-level behavior. Entity and field names are case-insensitive, an entity name of `*` is not supported, and grants from multiple roles combine. The policy loads at startup, so restart after changes. Startup fails if an entity, role or field list is empty. Environment variable form: `Mtpr__Authorization__FieldWrites__product__editor__0=title`.

The policy controls only fields supplied by the caller. It does not control reads, defaults, computed values or cascades.

## Diagnose access

| Result | What to inspect |
| --- | --- |
| Startup failure | HTTPS authority, nonempty audience, and spelling of configured operations. |
| HTTP 401 | Bearer token presence, signature, issuer, audience and lifetime; issuer discovery and signing-key availability. |
| HTTP 403 | Principal role claims, role spelling and matching entity and operation grants; restart after changing grants. |
| Unexpected data access | Broad role grants, wildcard entity grants, direct database or engine access, and cascade behavior. |

Do not log or paste bearer tokens when troubleshooting. Identity-provider settings and role claims should be checked against the actual deployment. If permissions are changed or removed at the provider, account for the lifetime of already issued tokens.

## Data protection and recovery

The API does not implement field read permissions, row filters or tenant isolation. An entity-level grant covers all records for that operation; mutation responses can include fields without a separate read grant. SQLite cascades and set-null changes do not check grants on related entities. Restrict direct database and engine access because endpoint authorization does not apply there.

Successful direct writes add snapshots to `__mtpr_audit` in the same transaction. The table contains potentially sensitive values and is protected only by the database's access controls. It has no built-in retention, query API, independent storage or tamper detection. Cascades, direct SQL, reads and failed or denied requests are not logged. Subject and issuer can be null. Existing rows created before the issuer column was added retain a null issuer; do not infer their issuer. The column is added on the next successful direct write. Plan storage capacity and backups for growth; if audit insertion fails, the corresponding write rolls back. Do not treat this partial log as a complete audit trail.

Back up the database and matching definitions together, along with a separately protected copy of the deployment's authentication and authorization configuration. Stop the API and other writers before restoring. Verify authentication, allowed and denied operations, and representative data in an isolated copy before resuming service. Refer to the M3 operations guide for definition reloads and SQLite recovery details.
