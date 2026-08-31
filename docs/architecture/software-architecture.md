# Software Architecture

| Field | Value |
|---|---|
| Architecture style | Domain-oriented modular monolith |
| Delivery organization | Vertical slices inside business modules |
| Dependency policy | Clean Architecture direction at module/domain boundaries |
| Domain modeling | DDD applied selectively to complex financial behavior |
| Initial deployment | One ASP.NET Core API, one worker process when needed, one PostgreSQL 18 database |
| Persistence provider | EF Core 10 with `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x |
| Status | Version 1 baseline |
| Last reviewed | 2026-08-31 |

## 1. Executive decision

Fintech Backend Lab will begin as one deployable ASP.NET Core application composed of explicit business modules. Each module owns its behavior, data schema, persistence mapping, public contracts, and vertical use cases. The financial write path remains inside one process and one PostgreSQL transaction. [ADR 0006](../adr/0006-use-postgresql-and-npgsql.md) governs the current engine/provider decision.

This is not an all-in-one “big ball of mud.” The monolith is a deployment decision; modularity is a design decision. Compile-time dependencies, architecture tests, schema ownership, internal visibility, and code review enforce separation.

Microservices are deferred until measured independent deployment, scaling, resilience, or team-ownership needs exceed their operational cost.

## 2. Architecture drivers

The architecture is shaped primarily by:

1. atomic and balanced money movement;
2. immutable and reconcilable ledger history;
3. safe retries and concurrent requests;
4. resource-level authorization;
5. complete audit and diagnostic traceability;
6. a learning path that one developer can understand and operate;
7. a public repository that can evolve without false production claims;
8. the ability to extract a proven module later without designing a distributed system now.

The prioritized quality scenarios are defined in [Quality Attributes](../quality-attributes.md).

## 3. Architectural concepts and their separate responsibilities

### 3.1 Modular monolith

Defines business boundaries and the unit of deployment.

- One initial API deployment.
- Modules are not arbitrary folders; each owns a business capability.
- No cyclic module references.
- Cross-module access uses explicit contracts.
- Modules do not directly update another module's tables.
- Independent service extraction remains possible but is not promised.

### 3.2 Vertical slices

Define how a use case is organized.

A slice contains the smallest cohesive set of transport, validation, authorization, application orchestration, response mapping, and tests needed for one operation. Examples include `OpenWallet`, `CreateTransfer`, and `GetStatement`.

Slices avoid a global technical-folder design in which every controller, service, repository, and DTO is separated from the feature it serves.

### 3.3 Clean Architecture dependency direction

Defines what business rules may depend on.

- Domain behavior does not depend on HTTP, ASP.NET Core, EF Core, Npgsql, PostgreSQL, queues, telemetry exporters, or UI code.
- Application/use-case code coordinates domain behavior and depends on abstractions/contracts.
- Infrastructure implements persistence and external adapters.
- API and worker processes compose modules and infrastructure at runtime.

Clean Architecture does not require an interface for every class or a fixed number of projects.

### 3.4 Selective DDD

Defines how complex business rules are modeled.

Use rich modeling for money, wallet lifecycle, journal entries, postings, transfer state, idempotency, limits, and reversals. Use direct projections and simple handlers for read-only lists and administrative lookup where no rich invariant exists.

## 4. Runtime building blocks

### 4.1 API process

Responsibilities:

- HTTPS termination awareness and secure HTTP behavior;
- authentication and request identity;
- routing, model binding, transport validation, and Problem Details;
- endpoint-level and resource-level authorization;
- module dispatch/composition;
- correlation, tracing, metrics, and structured logging;
- liveness and readiness endpoints;
- Development-only OpenAPI/Scalar exposure according to repository policy.

The API must not contain core financial business rules in controllers or middleware.

### 4.2 Module assemblies

Responsibilities:

- domain rules and state transitions;
- application use cases;
- module-owned contracts;
- module-owned persistence mappings and migrations;
- module telemetry and audit contributions;
- internal registration with the composition root.

Most types should be internal unless another module genuinely needs a stable contract.

### 4.3 Worker process

Added when asynchronous work begins.

Responsibilities:

- claim and process outbox messages;
- apply bounded retry/backoff policies;
- use consumer-side deduplication where side effects require it;
- deliver fictional notifications through adapters;
- emit backlog, age, retry, failure, and dead-letter metrics;
- stop safely on cancellation without corrupting message state.

The worker never decides whether a transfer financially succeeded. It reacts to committed facts.

### 4.4 PostgreSQL 18

Responsibilities:

- durable module-owned records;
- atomic transactions;
- unique/check/foreign-key constraints within ownership boundaries;
- concurrency primitives and indexes;
- migrations and recovery practice;
- outbox durability.

PostgreSQL is a correctness participant, not a passive storage bucket. EF Core uses the Npgsql 10.x provider with explicit schema/type mappings and separate migration history per module context. The schema, role, timestamp, index, and MVCC policies are specified in [Data Architecture](data-architecture.md).

### 4.5 Optional future infrastructure

- Aspire for local orchestration when multiple processes/resources justify it;
- RabbitMQ after database outbox semantics are proved;
- Redis only for a measured cache or distributed-coordination need;
- managed application/container hosting and managed PostgreSQL for the public sandbox;
- an external standards-based identity provider during the authentication phase.

None is required to create the first domain or API slice.

## 5. Business modules

### 5.1 Identity and Access

**Owns:** authentication integration, application identity mapping, claims/roles/policies, credential/session configuration, and privileged identity boundaries.

**Does not own:** customer profile, KYC business status, wallet ownership, transfer rules, or ledger data.

Authentication infrastructure may be hosted by the API or an external provider. Other modules consume a small caller identity abstraction and authorization decisions rather than parsing tokens.

### 5.2 Customers

**Owns:** customer profile, customer lifecycle, simulated KYC submission/decision, and customer status.

**Publishes/contracts:** customer eligibility/status facts required by Accounts or Payments.

**Must not:** store credentials or write wallet/ledger tables.

### 5.3 Accounts

**Owns:** customer-facing wallet identity, owner reference, currency, lifecycle state, and product eligibility.

**Publishes/contracts:** wallet ownership, currency, and current lifecycle state.

**Must not:** independently mutate booked financial balance or create ledger postings.

### 5.4 Ledger

**Owns:** chart of accounts, ledger accounts, journal entries, postings, balance derivation/reconciliation, and accounting references.

**Publishes/contracts:** financial posting operations and read views required by Payments/Reporting.

**Must not:** depend on HTTP or Notifications; edit posted history; infer customer authorization from raw tokens.

Ledger is the most protected module and has no dependency on Payments.

### 5.5 Payments

**Owns:** transfer/funding/reversal operations, operation states, idempotency records, orchestration, and links to ledger entries.

**Depends on contracts from:** Accounts, Ledger, Risk, caller authorization context, audit/outbox infrastructure.

**Must not:** write ledger tables directly or treat a notification as part of financial success.

### 5.6 Risk and Limits

**Owns:** configured amount/daily limits and simple deterministic sandbox decisions.

Version 1 uses rules, not machine learning. Risk receives explicit decision inputs and returns a decision/reason; it does not own money or identity.

### 5.7 Notifications

**Owns:** notification preferences/templates if introduced, message handling, adapter delivery, deduplication, and delivery status.

**Consumes:** integration events after commit.

**Must not:** cause a financial commit or roll it back.

### 5.8 Reporting

**Owns:** statement/query models and read optimization where a dedicated projection is justified.

**Must not:** become an alternate financial source of truth. Every derived value must be traceable/reconcilable to authoritative module data.

## 6. Allowed dependency direction

The intended business dependency graph is acyclic:

- API/Worker composition may reference module registration and public contracts.
- Accounts may query Customers through a contract for eligibility.
- Payments may use Accounts, Ledger, and Risk contracts.
- Reporting may consume read contracts/events from Accounts, Ledger, and Payments.
- Notifications consumes committed integration events.
- Ledger depends only on tiny domain-neutral building blocks.
- Risk should receive explicit inputs and avoid depending on Payments internals.
- No module references API.
- No domain model references another module's infrastructure.

If a new dependency creates a cycle, redesign the contract, move stable shared language to a tiny building block, or use a committed event. Do not solve cycles with a service locator.

## 7. Internal module structure

A module may use this conceptual organization without creating an assembly for every folder:

```text
Module/
  Contracts/             # Deliberately public commands, queries, results, events
  Domain/                # Entities, value objects, policies, domain events
  Features/
    UseCaseName/         # Request, validation, authorization, handler, response
  Infrastructure/        # EF Core mappings, DbContext, adapters, migrations
  Configuration/         # Module registration and options validation
```

Rules:

- Feature names describe business intent, not CRUD mechanics.
- Domain types preserve invariants and expose behavior rather than public setters.
- Transport DTOs, domain models, and persistence concerns are not the same type by default.
- Manual mapping is preferred until measured repetition justifies a mapping dependency.
- Add an abstraction only for a stable boundary, substitution need, or test seam.
- Do not add generic repositories merely to hide EF Core.
- Cross-cutting policies belong in focused pipeline/middleware behavior only when they are truly cross-cutting.

## 8. Command and query paths

### 8.1 Command path

1. API authenticates and binds the request.
2. Boundary validation rejects malformed or unbounded input.
3. Resource authorization establishes the caller's right to request the action.
4. A module application handler loads required authoritative state.
5. Domain behavior validates invariants and produces intended changes.
6. Infrastructure persists the changes inside the defined transaction.
7. The response is mapped to an external contract.
8. Telemetry records outcome and timings without prohibited data.

Expected business failures produce stable, documented error codes/statuses. Unexpected failures pass through centralized exception handling and never reveal internals.

### 8.2 Query path

Queries can use EF Core projection directly to response/read models when:

- no domain behavior is executed;
- ownership and authorization remain explicit;
- query shape is bounded and measured;
- module data ownership is respected;
- the result does not become an alternate write model.

Use no-tracking queries for read-only data and cursor pagination for changing, long statements.

## 9. Financial transaction boundaries

### 9.1 Internal transfer

The transfer transaction atomically persists:

- transfer operation and final committed state;
- ledger journal entry and postings;
- idempotency request fingerprint and stable result;
- required audit metadata;
- outbox event.

It does not include:

- notification delivery;
- HTTP response transmission;
- telemetry exporter delivery;
- external network calls;
- long-running operator/customer interaction.

The transaction is short, local to one PostgreSQL database, and tested with injected failures. Participating module contexts must explicitly share one Npgsql connection and database transaction; separate context saves are not automatically atomic. Coordinate sequentially through infrastructure while preserving each module's application/persistence contract. No module may directly edit another module's tables.

### 9.2 Read-before-write and concurrency

The exact PostgreSQL concurrency strategy is deferred until a real-engine spike measures and proves it. Evaluate a consistent per-account locking protocol, `Serializable` transactions with whole-transaction retries, or an atomically guarded and reconcilable spend-position update. A row token such as `xmin` alone cannot protect a balance derived from concurrent posting inserts. The selected approach must demonstrate no overdraft under concurrent requests; see [the concurrency requirements](data-architecture.md#9-concurrency-strategy-requirements).

### 9.3 External work

No external provider, broker, or notification call participates in the database transaction. Committed integration work is represented by an outbox record and processed asynchronously.

## 10. Event model

### 10.1 Domain events

- Internal to a module/application transaction.
- Express meaningful domain occurrences.
- May coordinate in-process behavior that is safe inside the transaction.
- Are not automatically public integration contracts.

### 10.2 Integration events

- Represent committed facts for other modules/processes.
- Use stable versioned contracts.
- Are written to the outbox in the source transaction.
- Contain identifiers and necessary facts, not private domain objects or secrets.
- Consumers assume at-least-once delivery and are idempotent.

Do not publish an event named as a completed fact before the database commit succeeds.

## 11. HTTP API architecture

### 11.1 Contract rules

- Version public routes explicitly, initially under `/api/v1`.
- Use nouns/resources in paths and HTTP methods for intent.
- Use dedicated request and response contracts.
- Document success and error responses in OpenAPI.
- Use Problem Details with stable project-specific error codes/extensions.
- Validate input length, range, page size, and required relationships.
- Do not expose stack traces, database details, internal type names, or authorization reasoning.
- Accept cancellation and propagate it until the point where abandoning work would create ambiguity; document commit behavior carefully.

### 11.2 Idempotency contract

Money-moving commands require `Idempotency-Key`.

The server:

- scopes keys to a documented caller/operation boundary;
- canonicalizes or fingerprints the relevant request;
- atomically claims/stores the request and result;
- returns the original stable response for an equivalent retry;
- rejects key reuse with a different request;
- defines retention and cleanup before public release.

### 11.3 Authentication and authorization

- Use framework/standards-based authentication; do not invent token formats or cryptography.
- Convert authenticated identity into a small caller context.
- Use policy authorization for roles/capabilities.
- Use resource authorization for customer-owned records.
- Deny by default.
- Distinguish unauthenticated, forbidden, conflict, validation, and not-found behavior without leaking sensitive existence.

## 12. Configuration and secrets

- Bind configuration to typed options and validate required production settings at startup.
- Keep environment-independent defaults in version control.
- Keep secrets in local user-secret facilities or a deployment secret store.
- Do not log configuration objects or connection strings.
- Separate Development, test, staging, and public-sandbox resources.
- Fail startup for missing security-critical configuration instead of silently falling back.
- Persist/share ASP.NET Core Data Protection keys appropriately before multi-instance or durable authentication scenarios.

## 13. Observability

Every important operation should make these values available where safe:

- trace/correlation identifier;
- operation/transfer identifier;
- module and use-case name;
- outcome category and stable error code;
- duration;
- retry/conflict count;
- database/outbox stage;
- no secret, token, raw identity document, or unnecessary personal field.

Use structured logs, OpenTelemetry traces/metrics, and health checks. Business metrics describe outcomes, not confidential amounts per customer.

## 14. Deployment architecture

### Initial local topology

- ASP.NET Core API process;
- PostgreSQL 18 local service or pinned container, administered through pgAdmin/`psql`;
- API client;
- worker added in the asynchronous phase;
- local OpenTelemetry-compatible dashboard/exporter when observability begins.

### Public sandbox topology

- one managed application/container deployment for API;
- one worker deployment if asynchronous work is enabled;
- one managed PostgreSQL database, with version/features verified before host selection;
- deployment secret store/identity;
- telemetry backend;
- ingress with HTTPS, quotas/rate limits, and restricted operations;
- fictional seeded data only.

The API is designed to become stateless. Scale-out requires shared database state, compatible Data Protection/authentication configuration, and concurrency/idempotency tests across instances.

## 15. Maintainability guardrails

- No module cycles.
- No cross-module table writes.
- No shared-kernel dumping ground.
- No infrastructure types in domain models.
- No financial logic in controllers, filters, mapping profiles, or database triggers.
- No generic “service” or “manager” names when a business use case can be named.
- No package without a documented problem, license, maintenance, and removal assessment.
- No cache before correctness, invalidation, and ownership are defined.
- No asynchronous financial truth before transactional/outbox semantics are understood.
- No microservice extraction without an ADR and operational evidence.
- No optimization without a reproducible baseline.

## 16. Evolution strategy

1. Build one complete internal-transfer slice inside the modular monolith.
2. Prove financial, authorization, idempotency, and concurrency behavior.
3. Add reporting, worker/outbox, observability, CI, and deployment.
4. Measure bottlenecks and change frequency.
5. Extract only a low-risk capability if independent deployment has a concrete benefit.

Notifications is the preferred first extraction experiment because it reacts to committed events and does not own financial truth. Ledger is deliberately the last candidate, not the first.

## 17. Decision and diagram map

- [ADR 0001 — Modular monolith](../adr/0001-use-a-modular-monolith.md)
- [ADR 0002 — Double-entry ledger](../adr/0002-use-a-double-entry-ledger.md)
- [ADR 0006 — PostgreSQL, Npgsql, and module-owned schemas](../adr/0006-use-postgresql-and-npgsql.md)
- [ADR 0003 — Original schema decision (superseded)](../adr/0003-use-module-owned-sql-schemas.md)
- [ADR 0004 — Atomic transfer and outbox](../adr/0004-use-an-atomic-transfer-transaction-and-outbox.md)
- [ADR 0005 — Backend-first controller API](../adr/0005-use-a-backend-first-controller-api.md)
- [Architecture diagrams](diagrams.md)
- [Data architecture](data-architecture.md)
- [Security architecture](security-architecture.md)

## 18. References

- [Microsoft: Common web application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [Microsoft: .NET application architecture guidance](https://learn.microsoft.com/en-us/dotnet/architecture/)
- [Microsoft: EF Core transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions)
- [Microsoft: EF Core concurrency handling](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
- [Npgsql EF Core 10](https://www.npgsql.org/efcore/release-notes/10.0.html)
- [PostgreSQL 18 transaction isolation](https://www.postgresql.org/docs/18/transaction-iso.html)
- [PostgreSQL 18 schemas](https://www.postgresql.org/docs/18/ddl-schemas.html)
- [Microsoft: ASP.NET Core security](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0)
- [OpenTelemetry for .NET](https://opentelemetry.io/docs/languages/dotnet/)
- [C4 model](https://c4model.com/)
