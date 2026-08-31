# ADR 0002 — Use a Double-Entry Ledger as the Financial Source of Truth

- **Status:** Accepted
- **Date:** 2026-08-30
- **Decision owners:** Repository owner

## Context

A wallet backend must explain every balance change and prevent application behavior from silently creating or destroying money. A mutable `Balance` column is easy to query but does not preserve the complete accounting cause of changes and is vulnerable to lost updates, manual edits, and incomplete correction history.

The learning goals include accounting invariants, reconciliation, atomicity, reversals, and auditability.

## Decision drivers

- balanced financial state;
- immutable, traceable history;
- safe correction/reversal;
- deterministic reconciliation;
- clear separation between product operations and accounting records;
- interview value grounded in a real financial concern.

## Decision

Use an append-oriented double-entry ledger as the source of truth for booked balances.

- Every posted journal entry contains at least two postings.
- Total debits equal total credits per journal entry and currency.
- Posted entries/postings are immutable.
- Business operations reference their journal entries.
- Reversals create new compensating entries referencing originals.
- Version 1 permits a full reversal only when the original recipient can return the full amount without overdraft.
- Balance is derived from postings.
- Any cached/projected balance is secondary, reconcilable, and rebuildable.
- Version 1 journal entries contain one currency; FX is unsupported.

## Alternatives considered

### Mutable wallet balance only

**Advantages:** simple schema and fast reads.

**Rejected:** weak traceability, easy accidental mutation, poor reconciliation, and corrections overwrite history.

### Single-entry transaction history plus balance

**Advantages:** more history than a balance-only model.

**Rejected:** does not independently enforce balanced sources/destinations and still allows accounting drift.

### Full event sourcing

**Advantages:** all state represented as events and replayable projections.

**Not selected:** unnecessary to achieve the required ledger guarantees and introduces broader event-sourcing complexity.

## Consequences

### Positive

- Every unit of fictional value has a balanced explanation.
- Reconciliation can detect drift or broken references.
- Corrections preserve original history.
- Accounting truth remains independent from read projections.

### Negative

- Accounting concepts must be learned and reviewed carefully.
- Statement and balance queries need appropriate indexes/projections.
- Database constraints alone cannot easily validate sums across posting rows.
- More tables and transaction logic exist than in CRUD balance storage.
- Chart-of-accounts/sign conventions must remain consistent.

## Guardrails

- No public/admin endpoint directly edits booked balance.
- No update/delete behavior for posted entries.
- Posting amounts and currency are explicit.
- Ledger does not authorize customers; callers prove authorization before requesting a posting.
- Reconciliation is independent from the normal posting code path where feasible.
- Accounting examples and tests use fictional funds only.

## Validation

- Unit tests reject unbalanced or invalid entries.
- SQL integration tests verify atomic persistence and structural constraints.
- Reconciliation after normal, retry, reversal, and concurrency tests reports zero unexplained difference.
- An intentional corruption exercise demonstrates detection and investigation.

## Review triggers

- new asset/currency scales or foreign exchange;
- pending/held/settled balance semantics;
- external settlement and reconciliation;
- fees, partial reversals, chargebacks, or disputes;
- measured ledger-query scale requiring projections/partitioning;
- any proposal for event sourcing.

## Related documents

- [Product rules](../product-brief.md#9-business-rules-and-invariants)
- [Data architecture](../architecture/data-architecture.md)
- [Double-entry diagram](../architecture/diagrams.md#10-double-entry-examples)
