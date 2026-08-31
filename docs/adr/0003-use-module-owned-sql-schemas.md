# ADR 0003 — Use One SQL Server Database with Module-Owned Schemas (Superseded)

- **Status:** Superseded by [ADR 0006](0006-use-postgresql-and-npgsql.md) on 2026-08-31
- **Date:** 2026-08-30
- **Decision owners:** Repository owner

> Historical record only. The original engine choice below is no longer implementation guidance. ADR 0006 selects PostgreSQL and preserves the one-database/module-owned-schema boundaries. This body is retained to avoid silently rewriting accepted decision history.

## Context

The modular monolith needs durable ownership boundaries while preserving a local atomic transaction for internal transfers. A database per module would introduce distributed consistency/operations too early. A single undifferentiated schema would make table ownership unclear and encourage arbitrary joins and writes.

## Decision drivers

- local ACID transaction for core financial operations;
- clear table/migration ownership;
- simple local development, backup, restore, and deployment;
- ability to evolve read models independently;
- reduced temptation for accidental cross-module coupling.

## Decision

Use one SQL Server database with a schema and EF Core `DbContext` boundary per important module where practical.

- Each table has one owner.
- Only the owner writes/migrates its tables.
- Cross-module behavior uses explicit contracts.
- Cross-schema foreign keys are avoided by default; exceptions require a new ADR.
- Cross-module references use opaque identifiers and owner validation.
- Reporting projections are read-only/rebuildable.
- Financial transaction coordination may share one database transaction without granting modules permission to edit each other's tables.

## Alternatives considered

### One database and one global schema/DbContext

**Advantages:** easiest queries and transaction handling.

**Not selected:** weak ownership, growing model coupling, migration conflicts, and easy cross-feature table access.

### Separate database per module

**Advantages:** strongest persistence isolation and service-extraction readiness.

**Not selected:** cross-database transaction/eventual consistency, more migrations/connections/backups, and unjustified operational cost.

### One physical database per environment but no schema ownership rule

**Advantages:** flexible and simple initially.

**Rejected:** flexibility would rely entirely on developer memory and reviews rather than explicit boundaries.

## Consequences

### Positive

- One backup/restore and one transaction manager initially.
- Table and migration ownership are visible.
- Module persistence can evolve independently within one database.
- Future extraction has a clearer data starting point.

### Negative

- Cross-module references may not have database foreign keys.
- Transaction coordination across multiple contexts needs careful integration testing.
- Reporting cannot casually join everything from arbitrary feature code.
- Same-server access still makes boundary violations technically possible.

## Guardrails

- Runtime database identity uses least privilege.
- Cross-module table access is caught by review/architecture/persistence tests where possible.
- Module schema names and migrations are explicit.
- Reporting access is isolated from command behavior.
- Shared SQL transaction coordination is infrastructure-level and documented.
- Direct SQL maintenance follows audited runbooks.

## Validation

- Clean migrations rebuild every schema.
- Each module can identify its owned tables and contracts.
- Core transfer commits across required schemas atomically under failure injection.
- No unrelated module writes another module's schema.

## Review triggers

- a module is extracted to a separate process;
- database permissions cannot enforce required separation;
- migration/deployment coupling becomes measurably harmful;
- read/reporting needs justify a separate store;
- SQL Server scale/availability design changes materially.

## Related documents

- [Data architecture](../architecture/data-architecture.md)
- [Module data ownership diagram](../architecture/diagrams.md#12-module-data-ownership)
- [ADR 0001](0001-use-a-modular-monolith.md)
