# M0 - Foundation Hardening

[Back to index](index.md)

## Architecture decision records(ADR)

### ADR 0001: SQLite for the initial runtime

Status: Accepted for the prototype; revisit before multi-user production deployment.

The engine currently uses SQLite, including foreign keys and transactional schema changes. Keep it as the MVP database to preserve the existing end-to-end slice and simple local setup. The persistence interfaces provide a boundary for future providers, but do not imply provider independence today. Concurrency, deployment and tenant isolation requirements must be evaluated before production use.

### ADR 0002: REST API for the initial runtime

Status: Accepted for the prototype.

Use the existing HTTP REST endpoints under `/api/{entity}` for CRUD and querying. Definitions drive behavior, and errors use MTPR codes. Authentication and authorization were added in M4. API versioning and a production compatibility policy remain future work.

### ADR 0003: Rule expression evaluation

Status: Superseded for the initial M3 subset; see the M3 implementation notes for the decision to use a bounded internal interpreter.

The original proposal was to use an existing, sandboxable expression evaluator, with JSON Logic as a candidate. M3 instead implemented a bounded internal interpreter for the supported rule subset; the draft schema is not an implementation commitment.

### ADR 0004: License review

Status: Pending owner decision.

The repository currently contains the GNU GPL version 3 license text. Preserve it until the copyright owner deliberately confirms or changes the license. GPL is a copyleft license; the choice has consequences for distribution and commercial use. The project owner should review the intended distribution model and obtain legal advice as appropriate before deciding. This record does not itself grant a license or change existing terms.

## MVP acceptance criteria

These five checks apply to the first usable, configuration-driven MVP, after security and business rules are implemented. Run them against the release candidate and retain the results.

1. From a clean checkout, a new contributor starts the sample API using documented steps in under 30 minutes.
2. Add an entity by editing definitions only; within five minutes, create, read, update and delete a record without changing code or rebuilding.
3. For the library sample, demonstrate at least one one-to-many and one many-to-many relation, filtering, sorting and paging; automated integration tests pass against SQLite.
4. Demonstrate at least one configured validation rule and one configured state transition without code changes; automated tests cover both passing and rejected cases.
5. Automated security tests show that an unauthorized role and a different tenant cannot read or modify a protected record, and the full CI restore, build and test workflow passes.

## Definition schema v0

`m0_foundation_hardening.schema.json` describes the current one-entity-per-file format. The version is identified by the schema's `$id`; existing definition files need no version property. The runtime does not currently validate against JSON Schema, and its verifier also enforces cross-field and cross-file constraints (including case-insensitive uniqueness, valid references, constraint ordering and set-null compatibility). JSON Schema alone is not a substitute for runtime validation.

The `role` and `rule` definitions in `$defs` remain drafts and are not runtime security policies. The runtime supports a separate, limited entity `rules` property as documented in the M3 implementation notes; HTTP role grants are configured separately as documented in M4. Relations in the implemented format are represented by `field.references` and join entities, rather than a separate relation file. The v0 schema is a documented authoring contract; loading a `$schema` property is optional and does not select a runtime version.

## Schema verification

`M0FoundationHardeningSchemaTests` uses JsonSchema.Net to validate all four library example definitions and reject ten negative examples, including missing properties, invalid identifiers and types, invalid references, incompatible constraints and duplicate unique fields. Run them with `dotnet test tests/Mtpr.Tests/Mtpr.Tests.csproj --filter FullyQualifiedName~M0FoundationHardeningSchemaTests`; they also run in the normal solution test and CI workflow. These tests exercise the entity schema, not the proposed role or rule definitions. Runtime verification still covers additional cross-field and cross-file constraints as described above.
