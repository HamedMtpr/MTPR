# Project Roadmap

> Status: Draft v0.1
> Scope: From the current scaffold to a first usable, configuration-driven backend generator.

- [Back to index](index.md)

## 1. Vision

MTPR is a configuration-driven, no-code backend generator. A backend for any application domain, with its own business rules, should be created through configuration alone, without writing code.

## 2. Guiding Principles

1. **Vertical slice before breadth.** Prove one end-to-end flow before adding features.
2. **Schema is the contract.** A versioned definition schema is the single source of truth.
3. **Start with one domain.** Generalize only after the first domain works. "Any domain" is the destination, not the starting point.
4. **Reuse before building.** Use mature libraries for rules, expressions, and migrations instead of inventing languages.
5. **Security from day one.** Authorization and tenant isolation belong in the data model.
6. **Every milestone has testable acceptance criteria.**

## 3. Architecture Pillars

| Pillar | Responsibility | Status |
|---|---|---|
| Definition Engine | Versioned schema for entities, fields, relations, roles, rules | Not started |
| Metadata-Driven Execution Core | Loads definitions and executes requests generically | Not started |
| Dynamic Data Layer | Persistence, migrations, querying from metadata | Not started |
| Business Rule and Workflow Engine | Validation, triggers, state transitions | Not started |
| Cross-cutting | Errors, auth, audit, observability | Partially started (exceptions) |

## 4. Milestones

### M0 - Foundation Hardening (Weeks 1-2)

**Goal:** Lock the contract and make the repository reliable.

- [ ] Choose and confirm the license deliberately (the current one affects commercial and internal use).
- [ ] Add `global.json` and `Directory.Build.props` (SDK pinning, shared analyzers, nullable, warnings as errors).
- [ ] Add CI (GitHub Actions): restore, build, test, on every PR.
- [ ] Write the first ADRs in `docs/planning/adr/` (target database, API style, rule engine choice).
- [ ] Draft the versioned JSON Schema for `Entity`, `Field`, `Relation`, `Role`, `Rule`.
- [ ] Define 5 measurable acceptance criteria for the MVP.

**Exit criteria:** CI is green, the schema v0 is committed, ADRs are merged.

### M1 - Vertical Slice (Weeks 3-5)

**Goal:** Prove the core hypothesis with one thin end-to-end path.

- [ ] Load an entity definition from a file.
- [ ] Generate the database table and migration automatically.
- [ ] Expose generic REST CRUD for that entity.
- [ ] Validate input from field metadata (type, required, length, range).
- [ ] Return errors using `MtprException` codes.
- [ ] Integration tests against a real database.

**Exit criteria:** A new entity added only by editing a definition file gets working CRUD, with no code change and no rebuild.

### M2 - Relations and Querying (Weeks 6-7)

- [ ] One-to-many and many-to-many relations.
- [ ] Filtering, sorting, paging, field selection.
- [ ] Referential integrity and delete behaviors.
- [ ] Schema evolution v1: safe additive changes (add field, add entity).

**Exit criteria:** A multi-entity sample domain works through the API.

### M3 - Business Rules (Weeks 8-9)

- [ ] Rule model: trigger (before/after create/update/delete), condition, action.
- [ ] Expression evaluation using an existing engine (for example JSON Logic or an established .NET expression library).
- [ ] Computed and default values.
- [ ] Simple state machine for entity lifecycle.

**Exit criteria:** Rules change behavior through configuration only, covered by tests.

### M4 - Security and Multi-Tenancy (Weeks 10-11)

- [ ] Authentication integration (OIDC/JWT).
- [ ] Role-based access control at entity and field level.
- [ ] Row-level filters.
- [ ] Tenant isolation strategy.
- [ ] Audit log of all changes.

**Exit criteria:** Security tests prove that unauthorized access and cross-tenant access are rejected.

### M5 - Alpha Release (Week 12)

- [ ] A realistic sample application (the first target domain) built purely from configuration.
- [ ] Quickstart in the README that works from a clean machine.
- [ ] Fill in the placeholder docs (building, testing, deployment).
- [ ] Container image and a basic deployment guide.
- [ ] Tag `v0.1.0-alpha` and write release notes.

**Exit criteria:** A new developer builds the sample app in under 30 minutes using only the docs.

## 5. Post-Alpha Backlog

- Destructive schema changes with migration planning.
- Workflow engine (multi-step, long-running, human tasks).
- Event/webhook integration and background jobs.
- Admin UI for editing definitions.
- Plugin/extension points for cases configuration cannot cover.
- Additional database providers.
- Observability: metrics, tracing, structured logs.
- Performance benchmarks.

## 6. Risks and Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Scope too broad ("any domain") | Critical | Pick one target domain for M1-M5 |
| Schema design changes late | High | Lock and version the schema in M0 |
| Runtime schema evolution complexity | High | Additive-only changes until after alpha |
| Custom rule language | High | Reuse an existing expression engine |
| Configuration ceiling (users need code eventually) | High | Design extension points early |
| Security added late | High | M4 is mandatory before alpha |
| Over-investing in infrastructure | Medium | Time-box M0; every milestone ships user-visible behavior |

## 7. Definition of Done (per milestone)

- Acceptance criteria met and demonstrated.
- Unit and integration tests pass in CI.
- Documentation updated in `docs/`.
- Public API and schema changes recorded in release notes.
- No open critical bugs.

## 8. Success Metrics

- Time to add a new entity with CRUD: under 5 minutes, with zero code.
- Sample app built from configuration only: 100% of its business rules.
- Test coverage of the core engine: 80% or higher.
- Time for a new contributor to run the project: under 30 minutes.

## 9. Review Cadence

Review this roadmap at the end of each milestone. Record changes in the commit history of this file.
