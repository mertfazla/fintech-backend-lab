# Domain and Architecture Glossary

This glossary is normative for Version 1. Code, API contracts, database names, diagrams, tests, and discussions should use these terms consistently.

## Financial domain

| Term | Meaning in this project | Common confusion to avoid |
|---|---|---|
| Amount | A quantity of one currency, represented in minor units. | An amount without currency is incomplete. |
| Available balance | Funds that may currently be spent after applying any modeled restrictions. In the first transfer slice it equals booked balance because holds are not yet modeled. | Do not promise hold/pending semantics before implementing them. |
| Booked balance | Balance derived from posted ledger entries. | It is not an independently editable field. |
| Clearing account | A platform ledger account used as the other side of a sandbox funding entry. | It is not a real bank account. |
| Credit | One side of a double-entry posting. Its effect depends on the ledger account type. | “Credit always increases money” is not universally correct. |
| Currency | The explicit monetary unit, such as TRY, USD, or EUR. | Never infer it from language, country, or display symbol. |
| Debit | One side of a double-entry posting. Its effect depends on the ledger account type. | “Debit always decreases money” is not universally correct. |
| Double-entry ledger | Accounting model in which every journal entry has balanced debit and credit postings. | It does not by itself solve authorization, idempotency, or concurrency. |
| Funds | Fictional monetary value represented by ledger postings. | Version 1 funds are not legal tender or claims on a real institution. |
| Journal entry | Immutable accounting record that groups balanced postings for one business event. | It is not the same thing as a customer-facing transfer. |
| Ledger | Append-oriented financial record that is the source of truth for booked balances. | An audit log is not a financial ledger. |
| Ledger account | Accounting bucket to which postings are applied. | A customer wallet is a product concept mapped to ledger accounting. |
| Money | Value object consisting of minor units and currency. | Do not represent money with `double` or as a naked number. |
| Minor unit | Smallest supported unit for a currency, such as kuruş or cent in Version 1. | Currency scales differ in the real world; Version 1 supports only declared currencies. |
| Posting | One debit or credit line in a journal entry against one ledger account. | A journal entry requires multiple postings. |
| Reconciliation | Process of comparing financial representations and detecting unexplained differences. | It detects inconsistencies; it must not silently rewrite history. |
| Reversal | A new compensating financial operation that offsets an eligible original operation. | It is not an update or deletion of the original entry. |
| Settlement | Final exchange of value between parties/providers. | Version 1 has no external settlement; internal posting is not bank settlement. |
| Transfer | Business operation that moves funds between two internal wallets. | It is not identical to the journal entry that accounts for it. |
| Wallet | Customer-facing account in one currency with ownership and lifecycle state. | It is not a mutable balance row and not necessarily a legal bank account. |

## Reliability and consistency

| Term | Meaning in this project |
|---|---|
| Atomicity | All state changes inside a defined transaction commit together or none commit. |
| Concurrency conflict | Overlapping operations attempt changes that cannot both preserve invariants. |
| Concurrency token | Value used to detect whether persisted state changed since it was read. |
| Dead-letter | Message that cannot be processed after the defined retry policy and requires inspection. |
| Exactly-once effect | One business effect despite possible retries or duplicate delivery; achieved through transactions and idempotency, not assumed from transport. |
| Idempotency key | Client-supplied identifier that gives repeated equivalent commands one stable outcome. |
| Inbox | Consumer-side record used to detect and safely ignore already processed messages. |
| Outbox | Messages stored atomically with business state, then delivered asynchronously by a worker. |
| Optimistic concurrency | Detect conflicting writes at save time rather than locking first. |
| Retry | Another attempt after a defined transient failure. It must be bounded and safe for the operation. |
| Transaction boundary | Exact set of state changes that must commit or roll back together. |
| MVCC | PostgreSQL's multiversion concurrency model. Consistent row visibility does not automatically make a multi-row business invariant safe. |
| Serialization failure | PostgreSQL rejects a transaction whose outcome cannot satisfy the selected isolation guarantees; a safe bounded retry restarts the complete transaction. |
| `xmin` | PostgreSQL system transaction identifier exposed by Npgsql as an optional row-concurrency token; not a timestamp, permanent revision, or aggregate-balance lock. |

## PostgreSQL persistence terms

| Term | Meaning in this project |
|---|---|
| Npgsql | PostgreSQL .NET driver and EF Core provider used by infrastructure, never by the domain core. |
| Schema | Namespace inside the shared PostgreSQL database owned by a module; access is also governed by roles and object grants. |
| `search_path` | PostgreSQL's object-name lookup path; only trusted schemas belong in the runtime path. |
| Partial index | Index covering only rows matching a predicate, useful for a tested conditional uniqueness/access rule. |
| `timestamptz` | PostgreSQL instant type used for UTC business events; it does not retain an original time-zone name or offset. |
| WAL | PostgreSQL write-ahead log, relevant to durability, replication, and point-in-time recovery. |
| pgAdmin / `psql` | Graphical/command-line PostgreSQL clients; neither is the database engine. |

## Security

| Term | Meaning in this project |
|---|---|
| Authentication | Establishing the caller's identity. |
| Authorization | Deciding whether the authenticated caller may perform an action. |
| Object-level authorization | Checking access to the specific wallet, transfer, statement, or customer—not only the endpoint. |
| Least privilege | Giving users, processes, and credentials only the access required for their responsibilities. |
| PII | Information relating to an identifiable person. Real PII is prohibited in this sandbox. |
| Secret | Credential or key whose disclosure grants capability, such as a password, API key, token, or connection-string password. |
| Threat model | Structured view of assets, trust boundaries, threats, controls, and residual risks. |
| Trust boundary | Place where data or control crosses between parties or environments with different trust assumptions. |

## Architecture

| Term | Meaning in this project |
|---|---|
| ADR | Versioned record of an important architecture decision, alternatives, and consequences. |
| Aggregate | Consistency boundary around domain objects changed together under domain rules. |
| Bounded context | Boundary inside which a domain model and language have a consistent meaning. |
| Clean Architecture | Dependency direction that keeps business rules independent from delivery and infrastructure details. |
| Contract | Explicit API or message shape through which modules or external clients interact. |
| DDD | Domain-driven design: modeling complex business rules with domain language and explicit boundaries. |
| Integration event | Stable fact shared outside the module after its transaction commits. |
| Modular monolith | One deployable application composed of enforced business modules. |
| Module | Cohesive business capability that owns behavior, data, contracts, and internal implementation. |
| Projection | Read-optimized representation derived from authoritative data. |
| Vertical slice | Organization around one end-to-end use case rather than one technical layer across the whole system. |

## Operational terms

| Term | Meaning in this project |
|---|---|
| Correlation ID | Identifier used to connect related operations and logs when a trace context is unavailable or supplemented. |
| Liveness | Whether the process is alive and should not be restarted solely due to dependency failure. |
| Readiness | Whether the instance can currently accept its intended traffic. |
| Recovery Point Objective (RPO) | Maximum targeted data-loss window for a recovery exercise. |
| Recovery Time Objective (RTO) | Maximum targeted service-restoration time for a recovery exercise. |
| SLI | Measured indicator of service behavior, such as successful transfer latency. |
| SLO | Internal target for an SLI; not a legal or customer SLA in this lab. |
| Trace | End-to-end record of work across API, database, and background processing. |
