# Documentation Index

This directory is the design source of truth for Fintech Backend Lab. The root `README.md` explains the learning journey; these documents define what the system is, why it is designed this way, and which rules implementation must preserve.

## Document status

The documents are a **Version 1 architecture baseline**. They are detailed enough to begin implementation, but they are not claims that the application already exists or is production-ready.

**Active persistence baseline (2026-08-31):** PostgreSQL 18, EF Core 10, and the Npgsql EF Core provider 10.x. [ADR 0006](adr/0006-use-postgresql-and-npgsql.md) supersedes the previous database-engine decision; the old ADR remains historical only. PostgreSQL schemas and type mappings are documented, but no database, executable schema script, or migration has been created.

When implementation reveals new evidence, update the relevant document and add or supersede an Architecture Decision Record (ADR). Never silently change a financial or security rule.

## Recommended reading order

1. [Product brief](product-brief.md) — users, problem, scope, use cases, rules, and release acceptance.
2. [Glossary](glossary.md) — the precise language used by the product and codebase.
3. [Quality attributes](quality-attributes.md) — measurable correctness, security, reliability, and maintainability goals.
4. [Software architecture](architecture/software-architecture.md) — architecture style, module boundaries, dependencies, runtime behavior, and evolution.
5. [Data architecture](architecture/data-architecture.md) — money representation, ledger model, ownership, consistency, indexing, and migrations.
6. [Security architecture](architecture/security-architecture.md) — trust boundaries, assets, authorization, threat model, and secure operations.
7. [Architecture diagrams](architecture/diagrams.md) — visual views of context, containers, modules, transactions, data, security, and deployment.
8. [Architecture decisions](adr/README.md) — decisions, alternatives, consequences, and review triggers.

## Hands-on implementation guides

- [01 — Create the project foundation yourself](tutorials/01-project-foundation.md) — solution creation, project references, complete starter snippets, Development-only Scalar, and HTTP functional tests. This is a manual walkthrough, not generated application code or a claim of a tested implementation.

## Source-of-truth map

| Question | Authoritative document |
|---|---|
| What does Version 1 do? | `product-brief.md` |
| What does a term mean? | `glossary.md` |
| How good must the system be? | `quality-attributes.md` |
| Which module owns a behavior? | `architecture/software-architecture.md` |
| Which module owns data? | `architecture/data-architecture.md` |
| Which database/provider should implementation use? | `adr/0006-use-postgresql-and-npgsql.md` |
| How is the system protected? | `architecture/security-architecture.md` |
| Why was a major choice made? | `adr/` |
| What should be built and learned next? | Root `README.md` |

## Documentation rules

- Use IDs for requirements and rules so tests and pull requests can reference them.
- Keep diagrams and prose consistent; prose wins if they temporarily disagree.
- Treat Mermaid diagrams as architecture views, not generated proof of implementation.
- Mark future elements clearly; do not draw planned services as if they are deployed.
- Record meaningful alternatives and consequences in an ADR.
- Supersede ADRs instead of rewriting decision history.
- Use fictional examples only.
- Never place secrets, tokens, personal data, or realistic financial credentials in documentation.
- Review product, data, and security documents when a money-moving flow changes.

## Review cadence

Review this documentation:

- before the first application project is created;
- before each roadmap phase begins;
- whenever a domain invariant or transaction boundary changes;
- before a public release or deployment;
- after a security incident exercise or meaningful production-like failure;
- before extracting a module into another process.
