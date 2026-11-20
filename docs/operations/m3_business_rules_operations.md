# M3 business rules: operations

- [Operations index](index.md)
- [M3 deployment](../deployment/m3_business_rules_deployment.md)
- [M3 build and run](../building/m3_business_rules_build.md)
- [M3 testing](../testing/m3_business_rules_testing.md)
- [M3 implementation details](../coding/m3_business_rules_implementation.md)
- [M2 operations](m2_relations_querying_operations.md)
- [M3 sample definition](../examples/m3-order/order.json)

## Definition changes

The catalog checks definitions on requests. Prepare complete files, verify them with the automated tests and keep the last working set. A malformed definition or a reference to an unknown field can prevent catalog reload and returns `MTPR-3001`. The database schema migrator only supports its existing additive changes; a definition change that violates those rules returns `MTPR-3002`. Definitions are outside the SQLite transaction, so changing several live files is not an atomic catalog update.

Rules and lifecycle changes affect subsequent requests without a rebuild. Rule triggers execute in definition order for the matching event. Before and after rules run inside the write transaction; rejection by either rolls back that operation. They apply to the directly requested entity only. SQLite cascades and set-null changes do not invoke related entities' rules or lifecycle checks. Business rules are not access control.

Lifecycle state and transition names are case-sensitive. Before removing a state, identify existing records that use it and plan how to move them through permitted transitions. A record whose stored state is absent from the active definition cannot be updated until the definition makes its state valid again. Restoring a prior definition may restore valid transitions but does not reverse data changes.

Computed decimal fields are read-only. Changes to expressions or newly appended optional computed fields do not backfill existing rows. GET, filtering and sorting use stored values, which may be old or null until each affected row receives a successful API update. Plan and verify a controlled backfill using API updates; there is no built-in bulk backfill. Recalculation and validation can fail, and rules or lifecycle constraints can reject an update. Avoid direct SQLite edits: they bypass engine validation, rules, lifecycle checks and recalculation.

## Diagnose responses

| Response | What to inspect |
| --- | --- |
| HTTP 422, `MTPR-2011` | The configured rejection rule and its trigger; update rules see the proposed complete record. |
| HTTP 422, `MTPR-2012` | The initial state, stored state and directed transitions. |
| HTTP 422, `MTPR-2000` with `MTPR-2013` | A client supplied a read-only computed field. |
| HTTP 422, `MTPR-2000` with `MTPR-2014` | A computed result overflowed or a required result was null; field constraint errors use their own codes. |
| HTTP 409, `MTPR-2009` | Reference or unique constraints and delete behavior. |
| HTTP 500, `MTPR-3001` | Definition syntax, fields, types, rule expressions, lifecycle or computed expressions. |
| HTTP 500, `MTPR-3002` | Unsupported schema change; restore the applied definition or use a supported additive change. |

## Backup and recovery

Stop the API and other writers; back up the SQLite database and matching definitions together. Restore both from the same backup set, then verify catalog loading, a representative record, a permitted transition and a rejected rule in an isolated copy. Do not replace a live database or definition set with a partial backup. If using another SQLite client for inspection, remember that application connections register foreign-key enforcement and the custom decimal collation; direct writes bypass the engine.
