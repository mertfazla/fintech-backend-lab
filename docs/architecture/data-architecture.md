# Data Architecture

## 1. Goals

The data design must make invalid financial state difficult to represent, preserve ownership boundaries, support deterministic reconciliation, and allow a clean database to be rebuilt from migrations.

The design is conceptual until implementation-specific migrations and measured query plans exist.

**Database baseline:** PostgreSQL 18 with EF Core 10 and `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x. Reviewed on 2026-08-31; see [ADR 0006](../adr/0006-use-postgresql-and-npgsql.md). This file specifies schemas and mappings; it does not create a database or executable migration.

## 2. Physical database strategy

Version 1 uses one PostgreSQL 18 database with a schema per business module. Pin a supported patch/image version when implementation begins, and use the same major version for development, integration tests, and deployment.

| Schema | Primary owner | Example conceptual records |
|---|---|---|
| `identity` | Identity and Access | Application user mapping, role/policy support where locally required |
| `customers` | Customers | Customer, KYC submission, KYC decision |
| `accounts` | Accounts | Wallet |
| `ledger` | Ledger | Ledger account, journal entry, posting |
| `payments` | Payments | Transfer, funding, reversal, idempotency record |
| `risk` | Risk and Limits | Limit configuration and decision record where persistence is needed |
| `notifications` | Notifications | Notification delivery and deduplication |
| `reporting` | Reporting | Rebuildable read projections, if introduced |
| `integration` | Platform/integration ownership | Outbox/inbox records |

Schema names are proposed conventions and may be refined before the first migration. Ownership rules are more important than exact names.

### 2.1 PostgreSQL schema and naming conventions

- Keep the declared module schemas: `identity`, `customers`, `accounts`, `ledger`, `payments`, `risk`, `notifications`, `reporting`, and `integration`.
- Use explicit lowercase `snake_case` mappings for application tables/columns/indexes. Do not assume Npgsql automatically converts C# names to this convention.
- Examples include `accounts.wallets`, `ledger.ledger_accounts`, `ledger.journal_entries`, `ledger.postings`, `payments.transfers`, `payments.idempotency_records`, and `integration.outbox_messages`.
- Explicitly map schema ownership for every EF Core model; do not let domain tables silently land in `public`.
- Give each module context its own migration history location, such as `accounts.__ef_migrations_history` and `ledger.__ef_migrations_history`. Configure it before generating migrations; do not share an undifferentiated history table across contexts.
- Review actual schema owners, grants, and `search_path` in each environment. Use qualified object names in reviewed raw SQL and keep untrusted writable schemas out of the search path.
- Separate owner/migration roles from non-owner runtime roles. Runtime roles get only required schema `USAGE` and object privileges; no superuser or schema-creation rights.
- A shared API runtime role may access several module schemas. Schema organization alone therefore does not enforce module isolation; contracts, tests, permissions, and reviews still matter.

These conventions use PostgreSQL's [schema and privilege model](https://www.postgresql.org/docs/18/ddl-schemas.html) and EF Core's [configurable migration history](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/history-table).

## 3. Data ownership rules

1. Each table has exactly one owning module.
2. Only the owner creates migrations for its tables.
3. Another module does not directly update the owner's tables.
4. Cross-module behavior uses application contracts.
5. Cross-module reporting uses approved read contracts, views/projections, or events—not opportunistic table joins inside unrelated write code.
6. Foreign keys and unique/check constraints are strong inside a module.
7. Cross-schema foreign keys are avoided by default because they create deployment and ownership coupling; any exception requires an ADR.
8. Cross-module identifiers are stored as opaque IDs and validated through the owner contract at the appropriate consistency point.
9. Reporting projections are disposable/rebuildable and never become financial truth.
10. Direct production-like manual edits to financial tables are prohibited; controlled repair requires a documented, auditable procedure.

## 4. Identifier strategy

- Use application-generated globally unique identifiers suitable for distributed creation and index locality, evaluated before implementation.
- Identifiers are opaque in API contracts; clients must not infer authorization or chronology from them.
- Business-operation IDs, journal-entry IDs, and trace/correlation IDs are distinct concepts.
- Database primary keys and public identifiers may be the same only when the trade-off is documented.
- Idempotency keys come from clients and are not entity identifiers.

Persist .NET `Guid` identifiers as PostgreSQL `uuid`. UUIDv7 is a candidate for insertion locality; the exact generation location and ordering contract remain an implementation decision, not an authorization mechanism. Verify index behavior on PostgreSQL and never substitute a UUID's implied time for an explicit business timestamp.

## 5. Money representation

Version 1 supports TRY, USD, and EUR as fiat currencies with two minor decimal places.

Conceptually, `Money` contains:

- signed 64-bit minor units (`long`/`bigint`), with domain and database checks enforcing positivity where the concept requires it;
- explicit currency code;
- checked arithmetic;
- comparison only within the same currency;
- no implicit currency conversion;
- explicit display formatting outside the domain core.

Rules:

- Commands accept a documented decimal-string or minor-unit contract; the API must not accept binary floating point.
- Domain money arithmetic is checked for overflow.
- Customer transfer amounts are strictly positive.
- Ledger postings store an absolute minor-unit amount plus debit/credit direction, or another equally explicit representation selected before migration.
- Database check constraints reject zero/invalid postings where applicable.
- Currency is constrained to the Version 1 supported set without relying on UI validation.

If the product later supports currencies with a different scale or cryptoassets, the representation must be revisited through a new ADR and migration plan.

### 5.1 PostgreSQL type mapping baseline

| Concept | .NET representation | PostgreSQL representation | Mapping rule |
|---|---|---|---|
| Entity/operation identifier | `Guid` | `uuid` | Explicit keys and owner references; do not store UUIDs as display strings. |
| Fiat amount in minor units | `long` | `bigint` | Checked arithmetic; positive command/posting constraints; never floating point or locale-dependent `money`. |
| Currency | Validated domain currency/code | `text` with a supported-value check | Allow only the declared uppercase Version 1 codes. |
| Business instant | UTC `DateTime`, or `DateTimeOffset` normalized to offset zero | `timestamp with time zone` (`timestamptz`) | Stores an instant, not an original time-zone identifier. |
| Calendar date without an instant | `DateOnly` | `date` | Use only where the business means a date, not an event timestamp. |
| Bounded reference or state | Validated string or mapped enum | `text` with appropriate length/value checks | Prefer explicit state checks initially; native PostgreSQL enums require a separate migration/versioning decision. |
| Boolean flag | `bool` | `boolean` | Required flags should not become tri-state accidentally. |
| Event payload or flexible metadata | Versioned serialization contract | `jsonb` when querying structure is useful, otherwise `text` | No authoritative ledger amounts hidden in JSON; bound and validate payloads. |
| Request fingerprint bytes | `byte[]` | `bytea` | Hash the defined normalized request contract, not incidental JSON formatting. |
| Optional row-conflict token | Infrastructure-mapped `uint` | Existing `xmin` system column | Not a user-created column, timestamp, durable business revision, or cross-row consistency guarantee. |

Time handling follows [Npgsql's timestamp rules](https://www.npgsql.org/doc/types/datetime.html). Normalize UTC at the boundary, retain a separate zone ID only if a future requirement needs it, and test round trips because PostgreSQL timestamp precision differs from .NET ticks.

## 6. Core conceptual model

### 6.1 Customer and KYC

**Customer**

- stable identifier;
- application-identity reference;
- lifecycle status;
- created/changed UTC instants;
- concurrency token where mutable state requires it.

**KYC submission**

- customer reference;
- submission state;
- fictional bounded input/reference only;
- submitted/decided instants;
- deciding operations actor and reason;
- immutable decision history or explicit transition records.

No real identity document is stored.

### 6.2 Wallet

- wallet identifier;
- owner customer identifier;
- currency;
- lifecycle state (`Active`, `Suspended`, `Closed` or a refined explicit set);
- ledger-account mapping identifier;
- created/changed UTC instants;
- concurrency token.

Important uniqueness/integrity:

- at most one active wallet per customer/currency according to the final state model;
- currency does not change;
- owner does not change;
- closed is terminal in Version 1.

Use a PostgreSQL partial unique index as a candidate for “one active wallet per customer/currency.” Define its state predicate explicitly and test concurrent creation, suspension/reactivation, and closure. If a new wallet opens while another is suspended, reactivation must reject the collision rather than violate uniqueness. The final lifecycle policy and index predicate must agree. See [PostgreSQL partial indexes](https://www.postgresql.org/docs/18/indexes-partial.html).

### 6.3 Ledger account

- ledger-account identifier;
- account code/type and normal-balance semantics;
- currency;
- customer wallet reference where applicable;
- lifecycle metadata;
- no directly writable authoritative balance field.

### 6.4 Journal entry

- journal-entry identifier;
- business operation type and identifier;
- effective/posted UTC instant;
- currency;
- description/reference safe for audit;
- immutable posting collection;
- reversal/original-entry references where applicable;
- created-by/correlation metadata.

### 6.5 Posting

- posting identifier;
- journal-entry identifier;
- ledger-account identifier;
- debit or credit direction;
- positive minor-unit amount;
- deterministic ordering within an entry.

The application validates debit/credit equality before persistence. Database design adds feasible structural constraints, while reconciliation provides an independent detective control. SQL check constraints alone cannot conveniently prove a sum across multiple rows.

### 6.6 Transfer

- transfer identifier;
- source/destination wallet identifiers;
- amount/currency;
- state;
- initiating customer/caller reference;
- created/completed/failed/reversed UTC instants as applicable;
- journal-entry identifier when posted;
- reversal link;
- stable failure category without sensitive internals;
- concurrency token where state transitions require it.

### 6.7 Idempotency record

- normalized caller/operation scope;
- idempotency key or secure hash where appropriate;
- request fingerprint;
- processing/completed state;
- operation identifier;
- stable response status/body or reconstructable result;
- created/completed/expiry instants;
- uniqueness constraint over the defined scope and key.

The record is written atomically with the completed business effect. A stale in-progress record requires a documented recovery rule; it cannot be guessed from elapsed time alone.

### 6.8 Outbox record

- message identifier;
- event type and schema version;
- occurred UTC instant;
- aggregate/business identifiers;
- serialized bounded payload;
- trace/correlation context safe to propagate;
- processing status, attempt count, next attempt, processed instant, and error summary;
- claim/lease metadata if concurrent workers are supported.

Outbox cleanup/archival follows retention and operational needs; deleting unprocessed messages is prohibited.

## 7. Ledger and balance model

### 7.1 Source of truth

Booked balance is calculated from posted ledger postings. A performance projection or cached balance may be introduced only if:

- its authority is explicitly secondary;
- every update occurs atomically or through a rebuildable projection;
- reconciliation compares it with postings;
- drift produces an alert and blocks unsafe automatic correction;
- repair rebuilds from ledger history rather than editing history.

### 7.2 Internal transfer accounting

For an internal same-currency transfer, one journal entry contains balanced postings that decrease the platform liability to the sender and increase the platform liability to the receiver. Exact debit/credit naming is confirmed in the accounting design/test vocabulary before implementation.

The essential invariant is not a UI sign convention; it is that total debits equal total credits and both wallet effects belong to one atomic entry.

### 7.3 Sandbox funding accounting

Sandbox funding posts between a declared sandbox clearing ledger account and the customer's wallet ledger account. This demonstrates balanced accounting without claiming external cash settlement.

### 7.4 Reversal accounting

A reversal creates a new journal entry with compensating postings and references the original journal entry/transfer. The original records remain unchanged. Because Version 1 forbids overdrafts and partial reversals, the original recipient must have enough available funds to return the full amount; otherwise the reversal is ineligible.

## 8. Transaction design

The core financial transaction includes:

- operation state;
- journal entry and postings;
- idempotency result;
- required audit information;
- outbox event.

All use the same PostgreSQL database transaction. When separate module contexts participate, explicitly share the same open Npgsql connection and database transaction through infrastructure coordination; multiple independent `SaveChanges` calls/connections are not automatically atomic. Execute participating contexts sequentially rather than concurrently on that connection. No HTTP response, external provider, broker publish, notification, or telemetry export is inside the transaction.

Keep the transaction short:

- perform bounded validation before opening it when safe;
- re-check authoritative mutable conditions inside it;
- avoid user interaction and network calls;
- order access consistently to reduce deadlock risk;
- treat commit ambiguity as an idempotency/recovery problem;
- retry only the complete safe operation under an explicit policy.

## 9. Concurrency strategy requirements

The chosen implementation must prove:

- competing transfers cannot overspend;
- equivalent duplicate requests create one effect;
- different requests remain independent;
- a state transition cannot overwrite a newer state silently;
- deadlock/concurrency failures return or retry according to a bounded policy;
- multi-instance API execution preserves the same guarantees.

PostgreSQL's default `Read Committed` isolation does not make a read-balance-then-insert-postings sequence safe from concurrent spending. Choose and test a strategy for the complete invariant, not merely an individual entity update.

Candidates for the real-PostgreSQL spike:

- Explicitly lock a stable per-account guard row with `FOR UPDATE`, using deterministic account order and reading the spend position after the lock is acquired. Every writer affecting the invariant must follow the same protocol.
- Use `Serializable` transactions with bounded whole-transaction retry for serialization failures.
- Use an atomically updated, reconcilable spend-position row with conditional updates/concurrency checks, committed alongside ledger postings. This is not permission to introduce an independently editable authoritative balance.

An Npgsql-mapped `xmin` token or application-managed token can detect conflicting updates to a row. It does not detect new posting rows or prevent aggregate-balance write skew by itself. Never treat `xmin` as permanent statement order or an externally durable revision.

Distinguish PostgreSQL serialization failure (`40001`), deadlock (`40P01`), and unique-constraint violation (`23505`) from each other and from EF row-concurrency exceptions. Roll back and retry only a bounded, safe complete unit when appropriate; duplicates/key conflicts require their own idempotency logic. See [PostgreSQL isolation](https://www.postgresql.org/docs/18/transaction-iso.html) and [Npgsql concurrency tokens](https://www.npgsql.org/efcore/modeling/concurrency.html).

For concurrent outbox workers, `FOR UPDATE SKIP LOCKED` is a candidate for short queue-claim transactions. Persist the claim/lease before doing external work and deduplicate delivery. Do not use skipped rows as an account-balance or financial-consistency strategy. See [PostgreSQL SELECT locking clauses](https://www.postgresql.org/docs/18/sql-select.html).

## 10. Indexing strategy

Indexes begin from access paths and constraints, not a blanket convention.

Start with B-tree indexes for ordinary equality/range/order access. Evaluate partial and covering indexes when a measured access path justifies them; use GIN only for an actual JSONB/array query. PostgreSQL does not keep a table permanently ordered by its primary key. Include statistics/autovacuum behavior in performance investigations rather than treating every slow query as a missing-index problem.

Initial candidates to validate with actual plans:

- unique scoped idempotency key;
- unique business-operation reference on journal entry;
- postings by ledger account plus stable chronological tie-breaker for statements;
- transfer by source/destination wallet and stable chronology;
- wallet by owner and currency/status;
- unprocessed outbox messages by status/next-attempt/creation order;
- KYC submissions by customer and current state.

For every nontrivial index document:

- query served;
- key and included columns;
- selectivity/cardinality assumption;
- read improvement;
- write/storage cost;
- measured execution plan before/after.

Use `EXPLAIN` for estimated plans and `EXPLAIN (ANALYZE, BUFFERS)` for measured execution on safe test workloads. `ANALYZE` executes the statement, including writes; never run it casually on real financial data. See [PostgreSQL EXPLAIN](https://www.postgresql.org/docs/18/using-explain.html).

## 11. Query and pagination rules

- Read only required columns through explicit projections.
- Use no-tracking queries unless state will be changed in the same unit of work.
- Avoid lazy loading and accidental N+1 queries.
- Bound page size and filter lengths.
- Use cursor pagination for statements and long changing histories.
- Cursor order includes a unique deterministic tie-breaker.
- Treat cursors as opaque and validate/tamper-protect as required.
- Never rely on database default row order.
- Measure generated SQL and execution plans with representative data.

## 12. Migrations and schema evolution

- Each module owns migrations affecting its schema.
- Generate PostgreSQL-specific migrations with `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x, not a dual-provider migration set. None exists yet in this repository.
- Keep each context's migration history in its owned schema and validate startup order for shared infrastructure/schema prerequisites.
- Migrations are reviewed SQL/change artifacts, not generated files to accept blindly.
- A clean database must be rebuildable from migration history.
- Destructive changes use expand/migrate/contract or another backward-compatible plan.
- Deployment defines when migrations run and which identity has schema permissions.
- Application runtime credentials should not automatically have broad schema-change permissions in a production-like environment.
- Seed only deterministic fictional reference/demo data.
- Never edit a migration already applied to a shared/released environment; create a corrective migration.
- Backup/restore and rollback/forward-fix behavior are rehearsed before public release.

Logical backup exercises use PostgreSQL tools such as `pg_dump` and `pg_restore` where applicable. Point-in-time recovery requires a separate physical/base-backup plus WAL strategy or a managed-provider equivalent; a one-off logical dump does not satisfy a continuous RPO target. Test actual restore and reconciliation before claiming recovery capability. See [PostgreSQL continuous archiving/PITR](https://www.postgresql.org/docs/18/continuous-archiving.html).

Integration tests use `Testcontainers.PostgreSql` against a pinned PostgreSQL 18 image, isolated per test run. Match important collation, schema, transaction, and extension settings with the intended deployment. Verify migrations, UTC round trips, partial uniqueness, duplicate requests, parallel spending, and outbox recovery on the real engine. See [the PostgreSQL Testcontainers module](https://dotnet.testcontainers.org/modules/postgres/).

## 13. Retention and deletion

Before public release, define retention for:

- idempotency records;
- audit records;
- outbox/inbox messages;
- notification delivery records;
- telemetry;
- fictional customer data.

Financial and audit records are not deleted casually. Because this is a sandbox, a full environment reset is preferable to inventing legally meaningful retention behavior. Real data-protection obligations require jurisdiction-specific legal and security review outside this project's claims.

## 14. Reconciliation

Reconciliation jobs/tests should verify at least:

- every journal entry balances by currency;
- every posting references a valid ledger account and journal entry;
- every completed financial operation references one posted journal entry;
- every successful reversal references one eligible original and one compensating entry;
- no original has multiple successful full reversals;
- any cached/projected booked balance equals ledger-derived balance;
- no unexplained orphaned outbox event or operation exists.

On mismatch:

1. emit a high-severity signal with safe identifiers;
2. prevent silent automated history edits;
3. preserve evidence;
4. investigate using a runbook;
5. repair through an auditable compensating or rebuild procedure.

## 15. References

- [Microsoft: EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [Microsoft: EF Core concurrency handling](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Microsoft: EF Core efficient querying](https://learn.microsoft.com/en-us/ef/core/performance/efficient-querying)
- [PostgreSQL 18 schemas and privileges](https://www.postgresql.org/docs/18/ddl-schemas.html)
- [PostgreSQL 18 indexes](https://www.postgresql.org/docs/18/indexes.html)
- [PostgreSQL 18 transaction isolation](https://www.postgresql.org/docs/18/transaction-iso.html)
- [Npgsql EF Core 10](https://www.npgsql.org/efcore/release-notes/10.0.html)
- [Npgsql date/time handling](https://www.npgsql.org/doc/types/datetime.html)
- [Npgsql concurrency tokens](https://www.npgsql.org/efcore/modeling/concurrency.html)
- [ADR 0006 — PostgreSQL and Npgsql](../adr/0006-use-postgresql-and-npgsql.md)
