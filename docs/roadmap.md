# Delivery Roadmap

This roadmap tracks the implementation of Fintech Backend Lab. It describes delivery milestones and their evidence, not a fixed calendar. A capability is complete only when it exists in runnable code, is covered at the appropriate test level, and is reflected in the documentation.

## Status

| Milestone | Status |
|---|---|
| 0. Repository and API foundation | In progress |
| 1. HTTP behavior and diagnostics | Planned |
| 2. PostgreSQL persistence | Planned |
| 3. Customer onboarding and access control | Planned |
| 4. Wallet and money model | Planned |
| 5. Ledger and internal transfers | Planned |
| 6. Idempotency, concurrency, and security | Planned |
| 7. Statements and operational reliability | Planned |
| 8. CI, containers, and deployment | Planned |

## Current baseline

The repository currently provides:

- a .NET 10 solution with API, Customers module, and functional-test projects;
- an ASP.NET Core controller host;
- built-in OpenAPI generation and Development-only Scalar;
- `GET /api/v1/system/status`;
- one passing HTTP functional test;
- a documented modular-monolith target and PostgreSQL decision;
- local Git history on the `main` branch.

PostgreSQL, authentication, customer workflows, wallets, transfers, and the ledger are not implemented.

## Milestone 0 — Repository and API foundation

Complete the repository baseline before adding business behavior.

Remaining work:

- pin the .NET SDK used by the repository;
- establish shared build and formatting settings;
- add the repository license and public contribution/security guidance;
- verify a clean clone can restore, build, and test;
- publish the initial repository after a secret and generated-file review.

Completion evidence:

- the solution builds without warnings;
- all tests pass from the repository root;
- generated output and local IDE state are not tracked;
- setup instructions work from a clean checkout.

## Milestone 1 — HTTP behavior and diagnostics

Establish consistent behavior at the API boundary before introducing persistence.

Deliverables:

- consistent Problem Details responses;
- centralized exception handling;
- request validation and cancellation behavior;
- correlation identifiers and structured logs without sensitive data;
- separate liveness and readiness endpoints;
- functional tests for successful and deliberate failure responses.

Completion evidence:

- Scalar exposes the intended development contract;
- automated tests verify status codes, response contracts, and error behavior;
- middleware order and endpoint exposure can be explained from the host configuration.

## Milestone 2 — PostgreSQL persistence

Introduce the first real persistence boundary through the Customers module.

Deliverables:

- PostgreSQL 18 development instance with repeatable setup instructions;
- EF Core 10 and the compatible Npgsql provider;
- a Customers-owned schema and `DbContext`;
- explicit mappings for identifiers, UTC timestamps, constraints, and indexes;
- module-specific migration history;
- integration tests against PostgreSQL rather than an in-memory substitute.

Completion evidence:

- a disposable database can be created from migrations and rebuilt from zero;
- generated SQL has been reviewed;
- important constraints are enforced by both application behavior and PostgreSQL.

## Milestone 3 — Customer onboarding and access control

Implement the first business vertical slice with fictional data only.

Deliverables:

- customer registration contract and domain rules;
- simulated onboarding/KYC state transitions;
- authentication mechanism selected and documented;
- policy and resource-level authorization;
- negative tests for cross-customer access.

Completion evidence:

- invalid state transitions are rejected;
- an authenticated customer cannot read or modify another customer;
- audit-relevant changes identify who performed the action and when.

## Milestone 4 — Wallet and money model

Introduce wallets without allowing money movement yet.

Deliverables:

- integer minor-unit money representation with an explicit currency;
- supported-currency validation;
- wallet ownership and lifecycle rules;
- one active wallet per customer and currency;
- database constraints for important invariants.

Completion evidence:

- floating-point types are absent from financial amounts;
- currency mismatches and invalid amounts are rejected;
- concurrent wallet creation cannot violate uniqueness.

## Milestone 5 — Ledger and internal transfers

Make the ledger the source of financial truth and build the first complete transfer workflow.

Deliverables:

- immutable journal entries and postings;
- balanced debit and credit rules;
- atomic transfer, ledger, audit, and outbox writes;
- transfer status and compensating reversal behavior;
- functional and PostgreSQL integration tests for success and rollback.

Completion evidence:

- every posted journal entry is balanced;
- partial transfers cannot persist;
- posted history is never edited to perform a reversal.

## Milestone 6 — Idempotency, concurrency, and security

Protect money-moving commands against retries, races, and unauthorized access.

Deliverables:

- idempotency-key handling with request-payload validation;
- a documented PostgreSQL concurrency strategy;
- bounded retry behavior where appropriate;
- transfer limits and abuse-resistant validation;
- redaction rules, rate limiting, and focused security tests.

Completion evidence:

- identical retries return the original outcome without duplicating money movement;
- an idempotency key cannot be reused with a different payload;
- concurrent commands cannot overspend a wallet;
- logs and responses contain no credentials or sensitive financial data.

## Milestone 7 — Statements and operational reliability

Add reliable read models and background processing.

Deliverables:

- cursor-paginated wallet statements;
- query projections and reviewed indexes;
- transactional-outbox processing with safe retries;
- reconciliation checks;
- logs, metrics, traces, and failure diagnostics.

Completion evidence:

- statement ordering remains stable while new records are added;
- query plans are recorded for important reads;
- failed background work can be retried without duplicate effects.

## Milestone 8 — CI, containers, and deployment

Make the repository reproducible and demonstrate the system in a hosted environment.

Deliverables:

- GitHub Actions restore, build, and test workflow;
- pinned PostgreSQL container configuration;
- dependency and secret scanning;
- documented configuration and secret handling;
- deployment, migration, rollback, and troubleshooting instructions;
- a public demonstration environment using fictional data.

Completion evidence:

- a clean CI environment reproduces the local build and tests;
- deployment does not run migrations through an overprivileged application role;
- rollback and recovery procedures have been exercised.

## Delivery rules

- Work on one bounded vertical slice at a time.
- Keep implemented and planned behavior clearly separated.
- Add a dependency only when it solves a demonstrated problem.
- Record material architectural changes in an ADR.
- Treat PostgreSQL integration tests as the evidence for PostgreSQL behavior.
- Never use real money, credentials, identity documents, or personal data.
- Do not describe the system as production-ready or compliant.
