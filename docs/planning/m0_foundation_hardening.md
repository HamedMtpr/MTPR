# M0 - Foundation Hardening

[Back to index](index.md)

## Architecture decision records(ADR)

### ADR 0001: SQLite for the initial runtime

Status: Accepted for the prototype; revisit before multi-user production deployment.

The engine currently uses SQLite, including foreign keys and transactional schema changes. Keep it as the MVP database to preserve the existing end-to-end slice and simple local setup. The persistence interfaces provide a boundary for future providers, but do not imply provider independence today. Concurrency, deployment and tenant isolation requirements must be evaluated before production use.

### ADR 0002: REST API for the initial runtime

Status: Accepted for the prototype.

Use the existing HTTP REST endpoints under `/api/{entity}` for CRUD and querying. Definitions drive behavior, and errors use MTPR codes. This keeps the first API consistent with the implemented slice. Authentication, authorization, API versioning and a production compatibility policy remain future work; this decision does not claim they exist.

### ADR 0003: Rule expression evaluation

Status: Proposed; selection deferred to M3.

Use an existing, sandboxable expression evaluator rather than a custom language. JSON Logic is the leading candidate, but a specific .NET implementation has not been selected or evaluated. Before acceptance, compare maintained libraries for supported operators, deterministic behavior, resource limits, security, license compatibility, and integration with the rule model. Rules are not currently executed; the draft schema is not an implementation commitment.

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

The `role` and `rule` definitions in the same schema are drafts only. Neither is loaded or enforced by the runtime. Their shapes may change before M3/M4 and must not be used for security decisions. Relations in the implemented format are represented by `field.references` and join entities, rather than a separate relation file. The v0 schema is a documented authoring contract; loading a `$schema` property is optional and does not select a runtime version.

## Schema verification

`M0FoundationHardeningSchemaTests` uses JsonSchema.Net to validate all four library example definitions and reject ten negative examples, including missing properties, invalid identifiers and types, invalid references, incompatible constraints and duplicate unique fields. Run them with `dotnet test tests/Mtpr.Tests/Mtpr.Tests.csproj --filter FullyQualifiedName~M0FoundationHardeningSchemaTests`; they also run in the normal solution test and CI workflow. These tests exercise the entity schema, not the proposed role or rule definitions. Runtime verification still covers additional cross-field and cross-file constraints as described above.
