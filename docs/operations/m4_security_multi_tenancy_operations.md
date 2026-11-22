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

The policy controls only fields supplied by the caller. It does not control defaults, computed values or cascades.

## Configure field read grants

Optionally restrict which fields a role may read under `Mtpr:Authorization:FieldReads`, with the same shape as field writes:

```json
{
  "Mtpr": {
    "Authorization": {
      "FieldReads": {
        "product": {
          "viewer": ["title"],
          "admin": ["*"]
        }
      }
    }
  }
}
```

The entity `read` grant is still required. For a configured entity, a GET list request is rejected with HTTP 403 if `fields`, `sort` or `filter` references a field that is not granted, since sorting or filtering would reveal hidden values. `id` is always readable. Responses of GET, POST, PUT and PATCH contain only granted fields, so a writer without a read grant still succeeds but sees only `id`. Roles without a read grant cannot read that entity; with no `fields` parameter the caller gets the granted subset. Entities without a policy are unchanged. Names are case-insensitive, an entity name of `*` is not supported, grants from several roles combine, and the policy loads at startup. Environment variable form: `Mtpr__Authorization__FieldReads__product__viewer__0=title`.

Limitations: error messages and validation failures can still mention hidden field names, DELETE and audit snapshots are not filtered, and direct database access is unaffected.

## Diagnose access

| Result | What to inspect |
| --- | --- |
| Startup failure | HTTPS authority, nonempty audience, and spelling of configured operations. |
| HTTP 401 | Bearer token presence, signature, issuer, audience and lifetime; issuer discovery and signing-key availability. |
| HTTP 403 | Principal role claims, role spelling and matching entity and operation grants; changes apply without restart, check the log for "Invalid ... policy ignored". |
| Unexpected data access | Broad role grants, wildcard entity grants, direct database or engine access, and cascade behavior. |

Do not log or paste bearer tokens when troubleshooting. Identity-provider settings and role claims should be checked against the actual deployment. If permissions are changed or removed at the provider, account for the lifetime of already issued tokens.

## Data protection and recovery

Tenant isolation applies only to entities listed under `Mtpr:Tenancy:Entities`; callers without the tenant claim receive 403 and an audited denial. Changing tenancy configuration applies without a restart, and existing rows need a correct tenant value before enabling it. Row filters apply only where configured. An entity-level grant covers all records for that operation. Field read grants hide fields only for configured entities. Cascade deletes require related entity delete grants, and set-null requires related update and field grants. Restrict direct database and engine access because HTTP authorization does not apply there.

For an indirect-write denial, inspect the `field-write:indirect` audit detail and the grants for the affected entities and fields. Configured field write grants must cover server-generated non-null values on create and computed changes on update, including tenant stamps. Field projection alone does not prevent inference through computed values, rules or validation errors. Policy sections reload independently; verify the effective policy after configuration changes.

Writes to a reference graph containing both scoped and unscoped entities are denied with `tenant:unsafe-definition`, as are requests on a tenant entity whose configured field is missing, computed or not a string, or whose uniqueness group omits the tenant field. Fully scoped references are checked transactionally; deletes that would affect a different tenant fail. Reads still require the configured entity and row permissions. Run a single API process per database: the catalog lease does not coordinate other processes, and it serializes API requests. Removing a tenancy policy removes the restriction. Verify the effective policy and protect configuration from unauthorized changes.

Committed changes to managed entity tables add snapshots to `__mtpr_audit` in the same transaction, including independent SQL after definition migration. The table contains potentially sensitive values and is protected only by the database's access controls. It has no built-in retention, query API, independent storage or tamper detection. Reads, 401 responses, rolled-back writes, schema changes and changes to other tables are not recorded. External SQL entries have `external-sql` detail and no trusted actor or request; their values use SQLite storage representations. Existing rows created before the issuer column was added retain a null issuer; the column is added during migration. Plan storage capacity and backups for growth; if audit insertion fails, the corresponding write rolls back. API cascades include `caused_by`, denied requests include the reason in `detail`, and API `request_id` values correlate entries. Triggers block UPDATE and DELETE on the audit table; retention requires deliberate administrative intervention and a backup.

Keep audit and guard triggers intact and ensure `__mtpr_audit_store_write` is empty outside record-store transactions. Restrict access to the SQLite file: a database owner can alter triggers, marker or audit data. Conflicting replacement and UPSERT operations are rejected; use explicit updates or deletes. Before allowing external SQL, load definitions to initialize the audit schema. External clients must enable foreign keys and register required collations.

Back up the database and matching definitions together, along with a separately protected copy of the deployment's authentication and authorization configuration. Stop the API and other writers before restoring. Verify authentication, allowed and denied operations, and representative data in an isolated copy before resuming service. Refer to the M3 operations guide for definition reloads and SQLite recovery details.

Row filters (`Mtpr:Authorization:RowFilters`) apply only to API requests on configured entities and reload when the configuration provider signals a change. Records outside the scope return 404. Creates and updates that produce an out-of-scope record roll back. Audit rows and cascaded changes are not filtered. Removing an entity policy removes its row restriction; verify policy changes with allowed and denied requests. An invalid reload retains the previous policy, which may be less restrictive than the intended change.
