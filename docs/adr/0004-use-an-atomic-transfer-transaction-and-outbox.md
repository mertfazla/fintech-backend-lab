# ADR 0004 — Use an Atomic Transfer Transaction and Transactional Outbox

- **Status:** Accepted
- **Date:** 2026-08-30
- **Decision owners:** Repository owner

**Implementation update (2026-08-31):** The database engine now follows [ADR 0006](0006-use-postgresql-and-npgsql.md). This ADR's atomicity and outbox decision remains accepted.

## Context

A transfer changes business-operation state and accounting truth. It must also support safe retries, auditability, and later notifications. If these records commit separately, a failure can produce a completed transfer without ledger entries, ledger entries without a stable API result, or committed money without a notification event.

Calling a notification or broker inside the SQL transaction would keep locks open, introduce a network dependency into financial success, and still not guarantee atomicity between systems.

## Decision drivers

- all-or-nothing financial state;
- safe client retry after timeout/commit ambiguity;
- reliable integration events without distributed transactions;
- short database transaction;
- background failure isolation;
- testable crash/recovery behavior.

## Decision

For a successful internal transfer, atomically persist in one PostgreSQL transaction:

- transfer operation/final state;
- balanced journal entry and postings;
- idempotency request fingerprint and stable result;
- required audit metadata;
- versioned outbox event.

After commit, a worker processes the outbox with at-least-once delivery assumptions, bounded retry/backoff, claim/lease safety, and idempotent downstream effects.

External notification/provider calls and telemetry export are outside the financial transaction.

## Alternatives considered

### Save transfer and ledger separately

**Rejected:** permits partial financial state and complicated repair.

### Publish directly to a broker after database commit

**Not selected:** process failure between commit and publish loses the event.

### Publish inside the database transaction

**Rejected:** ordinary database transactions cannot atomically commit a third-party broker/provider without distributed transaction complexity, and network calls lengthen locks.

### Distributed transaction coordinator

**Not selected:** unnecessary for the initial one-database design and incompatible with the desired portability/operational simplicity.

### Treat notification delivery as transfer completion

**Rejected:** notification availability must not redefine accounting success.

## Consequences

### Positive

- Financial state and retry result cannot diverge after commit.
- Committed integration events are not lost between database and worker.
- Notification outages do not roll back money movement.
- Crash/restart behavior can be exercised deterministically.

### Negative

- Outbox table, worker, cleanup, metrics, retry, and dead-letter behavior must be built.
- Delivery is at least once, so consumers/adapters must handle duplicates.
- “In progress” idempotency recovery needs an explicit policy.
- Transaction coordination across module-owned persistence requires careful implementation.

## Guardrails

- No completed integration fact is exposed before commit.
- No external network call inside the financial transaction.
- Every money-moving command has a defined idempotency scope.
- Retry is bounded and re-executes only a safe whole operation.
- Outbox payloads are versioned, bounded, and secret-free.
- Processed/dead-letter records follow explicit retention and replay rules.
- Replay is privileged and audited.

## Validation

- Failure injection at each transaction stage leaves either all or no required records.
- Client timeout after commit followed by retry returns the stored result.
- Worker crash before/after side effect results in no lost event and one logical side effect.
- Concurrent equivalent requests create one business effect.
- Outbox age/failure metrics expose stuck processing.

## Review triggers

- external bank/card workflows with pending settlement;
- multiple databases/services participate in financial workflow;
- broker becomes operationally required;
- outbox volume/retention becomes a measured bottleneck;
- notification/provider requires a different deduplication model.

## Related documents

- [Transfer transaction diagram](../architecture/diagrams.md#7-transfer-transaction-boundary)
- [Outbox lifecycle](../architecture/diagrams.md#13-outbox-processing-lifecycle)
- [Data architecture](../architecture/data-architecture.md)
