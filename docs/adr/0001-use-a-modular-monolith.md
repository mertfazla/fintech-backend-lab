# ADR 0001 — Use a Domain-Oriented Modular Monolith

- **Status:** Accepted
- **Date:** 2026-08-30
- **Decision owners:** Repository owner

**Implementation update (2026-08-31):** Engine references now follow [ADR 0006](0006-use-postgresql-and-npgsql.md). The modular-monolith decision is unchanged.

## Context

The product requires strongly consistent internal money movement, clear business boundaries, and enough architectural depth for interview practice. It is built initially by one developer and has no evidence of independent team ownership, per-module scaling, or deployment needs.

Starting with independently deployed services would require network contracts, service discovery, distributed tracing, message delivery, eventual consistency, deployment coordination, and additional failure recovery before the core financial model is proved.

An unstructured single project would be easy to start but would allow business and infrastructure concerns to become tightly coupled as the project grows.

## Decision drivers

- atomic financial operations inside one database transaction;
- low operational and local-development complexity;
- explicit, teachable business boundaries;
- ability to test and debug the complete system locally;
- maintainability as features grow;
- credible evolution path without speculative distribution.

## Decision

Use one deployable ASP.NET Core application organized as business modules: Customers, Accounts, Ledger, Payments, Risk and Limits, Notifications, and Reporting, with Identity and Access at the application boundary.

Inside the monolith:

- organize delivery as vertical slices;
- enforce Clean Architecture dependency direction;
- use DDD selectively for complex financial behavior;
- expose explicit module contracts;
- prohibit cyclic module references and cross-module table writes;
- use architecture tests and internal visibility to enforce rules;
- add a worker process only when asynchronous outbox processing begins.

## Alternatives considered

### Simple layered monolith

**Advantages:** lowest initial structure, familiar controllers/services/data layers.

**Not selected as the target:** business features tend to spread across global technical layers, module ownership becomes unclear, and unrelated changes can couple easily. Some layered ideas still exist inside modules.

### Microservices from the beginning

**Advantages:** independent deployment/scaling and strong process boundaries.

**Not selected:** there is no measured need, team split, or proven domain boundary to justify network and operational complexity. Distributed consistency would distract from mastering core correctness.

### Event-sourced system

**Advantages:** history-centric model and replay possibilities.

**Not selected:** event sourcing is not required to build an immutable double-entry ledger, and it adds event-versioning/projection/operational complexity beyond Version 1 goals.

## Consequences

### Positive

- Financial writes can use a local SQL transaction.
- One solution is easier to run, debug, test, and deploy.
- Business modules create portfolio evidence beyond folder naming.
- Boundaries can be improved as domain knowledge grows.
- Future extraction remains possible behind contracts/events.

### Negative

- The application deploys and initially scales as one unit.
- Boundary enforcement requires discipline and automated tests.
- One database can tempt cross-module shortcuts.
- A failure in one process can affect the entire API instance.
- Coordinating module-owned persistence in one transaction needs explicit design.

## Guardrails

- No module dependency cycles.
- No module directly updates another module's schema.
- Ledger has no dependency on Payments.
- Domain code has no ASP.NET Core, EF Core, Npgsql, PostgreSQL, or messaging dependency.
- Shared building blocks remain small and domain-neutral.
- Microservice language is not used to describe in-process modules.

## Validation

- Automated architecture tests verify declared dependencies.
- Dependency diagrams remain current.
- A complete transfer slice is implemented without cross-module table writes.
- A clean local environment runs the full application with documented steps.

## Review triggers

Reconsider only when evidence shows one or more of:

- a module requires materially different scaling;
- a separate team must own and deploy it independently;
- release coupling creates repeated measurable harm;
- security/isolation requires a process boundary;
- runtime reliability requires independent failure containment;
- the cost of extraction is lower than continuing in-process.

## Related documents

- [Software architecture](../architecture/software-architecture.md)
- [Architecture diagrams](../architecture/diagrams.md)
- [Quality attributes](../quality-attributes.md)
