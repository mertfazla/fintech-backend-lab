# ADR 0006 — Use PostgreSQL 18 and Npgsql with Module-Owned Schemas

- **Status:** Accepted
- **Date:** 2026-08-31
- **Decision owners:** Repository owner
- **Supersedes:** [ADR 0003](0003-use-module-owned-sql-schemas.md)
- **Refines:** The persistence implementation of ADR 0001 and ADR 0004; their modularity and atomicity decisions remain accepted.

## Context

The original documentation selected Microsoft SQL Server because it matched the owner's initially available tooling. The owner subsequently chose PostgreSQL and requested a complete documentation/schema-design migration before application implementation.

At this decision point the workspace contains documentation only. There is no application database, DDL script, EF model, migration, or production data to convert. This is a design change, not a tested runtime database migration.

The financial requirements do not depend on a particular relational engine: balanced immutable accounting, no overdraft, idempotent commands, atomic transfer state, auditability, and an outbox remain mandatory.

## Decision drivers

- Explicit owner preference for PostgreSQL.
- Compatible .NET 10/EF Core 10 provider support.
- One local relational transaction for the complete financial write path.
- Clear module data ownership without multiple databases or services.
- Useful PostgreSQL schema, SQL, MVCC, indexing, and operations practice.
- Reproducible development/CI/deployment on one database family.

## Decision

### Database and provider

- Use PostgreSQL 18 with a supported patch release, not a development/beta release.
- Use EF Core 10 with `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x; pin mutually compatible stable versions when implementation begins.
- Pin the database image/version used by development and CI; do not use a floating `latest` tag as the verification baseline.
- Use pgAdmin 4 for graphical administration and `psql` for command-line practice.
- Do not add dual-database support, generic dialect adapters, or a second migration set.

Npgsql's [EF Core 10 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html) document the provider release. PostgreSQL's [versioning policy](https://www.postgresql.org/support/versioning/) governs supported engine versions.

### Retained module/schema decision

- Keep one PostgreSQL database, not a database per module.
- Preserve `identity`, `customers`, `accounts`, `ledger`, `payments`, `risk`, `notifications`, `reporting`, and `integration` schema ownership.
- Each table and migration has one owner; consumers use contracts rather than cross-module table writes.
- Cross-schema foreign keys remain avoided by default under the existing boundary policy; exceptions require a separate ADR.
- Keep a context boundary per important module where practical, with explicitly configured module-specific migration history.
- Use explicit lowercase `snake_case` mappings and qualified raw SQL; govern schema owners, grants, and trusted `search_path`.
- Reporting projections remain rebuildable and cannot become financial truth.

### PostgreSQL-specific mapping and correctness

- Map identifiers to `uuid`, signed minor-unit amounts to `bigint`, and UTC instants to `timestamptz`.
- Retain supported-currency and positivity checks; do not use floating-point or PostgreSQL `money` for this model.
- Test conditional wallet uniqueness with PostgreSQL partial unique indexes.
- Select the complete spending-concurrency protocol through a real-engine spike. A row token such as `xmin` cannot protect a ledger balance derived from independently inserted postings on its own.
- Coordinate participating contexts on one shared Npgsql connection and transaction for transfer, ledger, idempotency, required audit data, and outbox.
- Keep network effects outside that transaction. Delivery remains at least once with idempotent processing.

Detailed rules and failure cases live in [Data Architecture](../architecture/data-architecture.md); this ADR does not prescribe untested implementation code.

### Verification and deployment

- Use `Testcontainers.PostgreSql` against the pinned PostgreSQL 18 image for real persistence/concurrency tests.
- Keep actual privileges, collation, extensions, and isolation assumptions visible in test/deployment configuration.
- Prefer managed PostgreSQL for the public sandbox; an Azure deployment uses Azure Database for PostgreSQL, subject to current version/feature/cost verification.
- Treat logical backups and point-in-time recovery as separate capabilities and rehearse restore/reconciliation.
- Do not claim PostgreSQL or pgAdmin has been installed or the database has been tested merely because these documents changed.

## Alternatives considered

### Keep the original engine

It satisfies the product's relational requirements but no longer matches the owner's chosen learning stack. There is no implemented database investment to preserve at this stage.

### Support both engines

Rejected for Version 1. Multiple providers, migrations, dialects, concurrency behaviors, and CI environments would increase scope without a product requirement.

### Replace the relational store with a document database

Not selected. The product deliberately needs relational modeling, balanced multi-record transactions, SQL practice, and constraints.

## Consequences

### Positive

- The documentation consistently reflects the chosen learning stack.
- Core domain and module boundaries stay unchanged.
- PostgreSQL-specific behavior is explicit rather than hidden by an ORM abstraction.
- No data-copy or migration-outage work is needed now because implementation has not begun.

### Costs and limitations

- The owner must learn PostgreSQL tooling, roles, MVCC, SQL, indexes, timestamp semantics, and recovery.
- Provider-specific migrations and tests are required.
- Database switching later would require a new design/data-migration effort; Clean Architecture does not make engines interchangeable.
- Core concurrency correctness and cloud availability remain unverified until their implementation phases.

## Validation gates

- All active product, roadmap, architecture, data/security, quality, and diagram guidance targets PostgreSQL.
- Legacy engine wording remains only in explicitly historical decision context.
- Documentation links/anchors/fences pass structural checks.
- Later implementation must prove clean migration, UTC/amount round trips, constraints, transaction rollback, concurrent spending, idempotency, and worker recovery on PostgreSQL.
- No application code, executable schema, or database configuration is created as part of this documentation change.

## Review triggers

- A required hosting environment does not support the selected engine major/features.
- A new supported major/provider version is being adopted.
- A proven performance, security, recovery, or product requirement needs another database design.
- A module is extracted into an independently deployed service.

## Related documents

- [Data architecture and PostgreSQL mappings](../architecture/data-architecture.md)
- [Software architecture](../architecture/software-architecture.md)
- [Security architecture](../architecture/security-architecture.md)
- [Architecture diagrams](../architecture/diagrams.md)
- [Quality attributes](../quality-attributes.md)
- [PostgreSQL Testcontainers module](https://dotnet.testcontainers.org/modules/postgres/)
- [Azure Database for PostgreSQL](https://learn.microsoft.com/en-us/azure/postgresql/overview)
