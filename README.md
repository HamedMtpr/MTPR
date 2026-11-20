# Project Summary

MTPR is a metadata-driven backend generator designed to turn configuration and business rules into backend systems for a range of application domains. The current prototype supports file-defined entities, SQLite-backed CRUD, relations and querying, and configurable defaults, rules, computed decimal fields and lifecycle transitions. The API requires JWT authentication, but role-based authorization, tenant isolation and audit logging remain planned; use it only in a trusted environment.

Configure `Mtpr:Authentication:Authority` as the HTTPS URL of an OpenID Connect issuer and `Mtpr:Authentication:Audience` as the intended API audience (for example, using the `Mtpr__Authentication__Authority` and `Mtpr__Authentication__Audience` environment variables). The API fails to start without both settings. All CRUD endpoints require a valid bearer token; authenticated users currently have unrestricted access to all entities and records.

- [Project roadmap](docs/planning/project_roadmap.md)
## Getting started

- [M0 foundation hardening](docs/planning/m0_foundation_hardening.md)
- [M1 vertical slice build](docs/building/m1_vertical_slice_build.md)
- [M2 relations and querying](docs/building/m2_relations_querying.md)
- [M3 business rules: build and run](docs/building/m3_business_rules_build.md)

## Documentation

- [Planning](docs/planning/index.md)
- [Code contribution](docs/coding/index.md)
- [Building](docs/building/index.md)
- [Releasing](docs/releasing/index.md)
- [Testing](docs/testing/index.md)
- [Deployment](docs/deployment/index.md)
- [Operations](docs/operations/index.md)
- [Monitoring](docs/monitoring/index.md)
