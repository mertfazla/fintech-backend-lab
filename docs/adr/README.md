# Architecture Decision Records

Architecture Decision Records preserve important choices, alternatives, consequences, and review triggers.

## Status meanings

- **Proposed:** under evaluation and not yet binding.
- **Accepted:** current baseline for implementation.
- **Superseded:** replaced by a newer ADR; retained as history.
- **Deprecated:** no longer recommended but may still exist during migration.
- **Rejected:** evaluated and deliberately not selected.

## Index

| ADR | Decision | Status |
|---|---|---|
| [0001](0001-use-a-modular-monolith.md) | Use a domain-oriented modular monolith | Accepted |
| [0002](0002-use-a-double-entry-ledger.md) | Use a double-entry ledger as financial source of truth | Accepted |
| [0003](0003-use-module-owned-sql-schemas.md) | Original module-schema/engine decision; historical record | Superseded by 0006 |
| [0004](0004-use-an-atomic-transfer-transaction-and-outbox.md) | Commit transfer, ledger, idempotency, audit, and outbox atomically | Accepted |
| [0005](0005-use-a-backend-first-controller-api.md) | Build a controller-based backend API before a frontend | Accepted |
| [0006](0006-use-postgresql-and-npgsql.md) | Use PostgreSQL 18 and Npgsql with module-owned schemas | Accepted; supersedes 0003 |

## ADR rules

- Use the next sequential number.
- Describe context before the chosen technology or pattern.
- Include viable alternatives and why they were not selected now.
- State both positive and negative consequences.
- Add measurable validation and review triggers.
- Never rewrite an accepted decision to hide history; supersede it.
- Link affected requirements, diagrams, and pull requests.

## Template

```text
# ADR NNNN — Decision title

- Status:
- Date:
- Decision owners:

## Context
## Decision drivers
## Decision
## Alternatives considered
## Consequences
## Guardrails
## Validation
## Review triggers
## Related documents
```
