# Product Brief — Fintech Backend Lab

| Field | Value |
|---|---|
| Product type | Educational digital-wallet and internal-payments backend |
| Working name | Fintech Backend Lab |
| Version | Version 1 architecture baseline |
| Status | Approved for learning implementation; not production approval |
| Primary platform | ASP.NET Core 10 Web API, EF Core 10/Npgsql 10.x, and PostgreSQL 18 |
| Data policy | Fictional sandbox data only |
| Last reviewed | 2026-08-31 |

## 1. Executive summary

Fintech Backend Lab is a backend-first sandbox in which fictional customers can register, complete simulated onboarding, open fiat wallets, receive sandbox funds, transfer funds to another customer, view statements, and receive a controlled reversal.

The product exists to teach and demonstrate the engineering properties that distinguish a financial backend from ordinary CRUD: balanced accounting, immutable history, explicit authorization, atomic transactions, idempotency, concurrency control, reconciliation, auditability, failure recovery, observability, and secure delivery.

The system does not move real money and must never be presented as a regulated or production-ready financial service.

## 2. Problem statement

A developer preparing for professional .NET backend work needs one coherent project that exercises API, database, security, architecture, testing, operations, and system-design skills together. Small CRUD projects do not naturally expose duplicate payment requests, concurrent spending, immutable accounting history, message-delivery failures, or resource-level authorization.

This product creates those problems in a safe sandbox where correctness can be specified and proven without connecting to real financial infrastructure.

## 3. Product vision

Build the smallest financial backend that can answer all of these questions with evidence:

- Where did every unit of money come from and go?
- Can any request create or destroy money accidentally?
- What happens when a client retries after a timeout?
- What happens when two transfers spend the same balance concurrently?
- Can one customer access another customer's wallet or statement?
- Can a completed transfer be corrected without rewriting history?
- Can an engineer trace, reconcile, test, deploy, and recover the system?

## 4. Product principles

1. **Correctness before features.** A smaller system with proved financial invariants is more valuable than a broad feature list.
2. **Ledger before displayed balance.** Posted ledger entries are the accounting source of truth.
3. **Immutable financial history.** Corrections create compensating entries; they do not edit or delete posted history.
4. **Secure by default.** Authentication is insufficient without authorization and ownership checks.
5. **Explicit failure behavior.** Timeouts, retries, conflicts, and partial failures are designed, not treated as surprises.
6. **Measured quality.** Tests, query plans, traces, and recovery exercises provide evidence.
7. **Honest scope.** Fictional data, simulated providers, known gaps, and non-production status remain visible.
8. **Evolution through boundaries.** Modules can evolve independently inside one deployment without speculative distribution.

## 5. Actors

### 5.1 Customer

A fictional end user who can:

- register and authenticate;
- view and maintain the allowed parts of a customer profile;
- submit simulated KYC information;
- open supported wallets after approval;
- view owned wallets, balances, transfers, and statements;
- send an internal transfer from an owned wallet;
- never access or operate another customer's wallet.

### 5.2 Operations user

An authorized fictional employee who can:

- review and decide simulated KYC submissions;
- perform controlled sandbox funding;
- inspect transfer and audit information required for support;
- request an eligible full reversal;
- never edit or delete posted ledger history;
- never bypass a policy without an auditable, explicitly modeled action.

### 5.3 System worker

A non-human process that can:

- process transactional outbox messages;
- deliver fake notifications;
- retry transient failures safely;
- reconcile selected financial data;
- publish operational metrics;
- never decide financial success independently from the committed source of truth.

## 6. Assumptions and constraints

- Version 1 supports fictional fiat funds only.
- Supported currencies are TRY, USD, and EUR.
- Each wallet has exactly one currency.
- A customer may have at most one active wallet per supported currency in Version 1.
- Cross-currency transfers and foreign exchange are not supported.
- Customers must have approved simulated KYC before opening or funding a wallet.
- Customers cannot overdraw a wallet.
- Funding is a privileged sandbox operation, not an external bank deposit.
- Transfers occur only between wallets inside this platform.
- Full reversal is supported; partial reversal is deferred.
- One ASP.NET Core deployment and one PostgreSQL 18 database form the initial runtime; use a supported, pinned patch version.
- Module-owned schemas and explicit contracts preserve boundaries inside that deployment.
- A frontend is optional and cannot own financial rules.
- All examples and seeded records must be obviously fictional.

## 7. Version 1 capabilities

### 7.1 Customer and onboarding

| ID | Requirement |
|---|---|
| FR-CUS-001 | A user can create a fictional customer account through the approved authentication flow. |
| FR-CUS-002 | A customer can submit a simulated KYC application. |
| FR-CUS-003 | An operations user can approve or reject a pending simulated KYC application. |
| FR-CUS-004 | KYC state changes are timestamped and auditable. |
| FR-CUS-005 | An ordinary customer cannot make an operations decision. |

### 7.2 Wallets

| ID | Requirement |
|---|---|
| FR-ACC-001 | An approved customer can open a wallet in a supported currency. |
| FR-ACC-002 | A customer can list and read only owned wallets. |
| FR-ACC-003 | An operations user can suspend or close a wallet through an explicit state transition. |
| FR-ACC-004 | A suspended or closed wallet cannot originate a transfer. |
| FR-ACC-005 | A wallet exposes booked and available balance semantics defined by the glossary. |

### 7.3 Sandbox funding

| ID | Requirement |
|---|---|
| FR-FND-001 | Only an authorized operations user can request sandbox funding. |
| FR-FND-002 | Funding requires an idempotency key. |
| FR-FND-003 | Successful funding creates a balanced ledger entry using a sandbox clearing account. |
| FR-FND-004 | Repeating the same funding request does not credit the wallet twice. |

### 7.4 Internal transfers

| ID | Requirement |
|---|---|
| FR-TRF-001 | A customer can transfer a positive amount from an owned active wallet to a different active wallet in the same currency. |
| FR-TRF-002 | A transfer request requires an idempotency key. |
| FR-TRF-003 | Authorization, wallet state, currency, limits, and funds are checked before posting. |
| FR-TRF-004 | A successful transfer and its ledger posting are committed atomically. |
| FR-TRF-005 | A completed transfer exposes a stable identifier and safe status representation. |
| FR-TRF-006 | A customer can read only transfers they are authorized to view. |
| FR-TRF-007 | Concurrent requests cannot produce an overdraft or duplicate posting. |

### 7.5 Statements and history

| ID | Requirement |
|---|---|
| FR-STM-001 | A customer can request a cursor-paginated statement for an owned wallet. |
| FR-STM-002 | Statement ordering is stable and documented. |
| FR-STM-003 | A statement item can be traced to its source journal entry and business operation. |
| FR-STM-004 | Posted financial history is not changed by normal correction or reversal flows. |

### 7.6 Reversals

| ID | Requirement |
|---|---|
| FR-REV-001 | Only an authorized operations user can request a reversal. |
| FR-REV-002 | Only an eligible completed transfer can be fully reversed. |
| FR-REV-003 | A reversal creates a new balanced compensating journal entry. |
| FR-REV-004 | The original transfer and journal entry remain immutable. |
| FR-REV-005 | A transfer cannot be reversed more than once. |
| FR-REV-006 | The reversal records actor, reason, time, correlation, and original transfer reference. |
| FR-REV-007 | A reversal cannot overdraw the original recipient; insufficient available funds make it ineligible in Version 1. |

### 7.7 Notifications and operations

| ID | Requirement |
|---|---|
| FR-OPS-001 | A committed transfer creates an outbox message in the same database transaction. |
| FR-OPS-002 | A worker processes messages with safe retries and duplicate protection. |
| FR-OPS-003 | Notification delivery is simulated through a fake adapter. |
| FR-OPS-004 | Operational endpoints distinguish liveness from readiness without exposing sensitive detail. |
| FR-OPS-005 | Important operations carry a correlation/trace identifier. |

## 8. Primary use cases

### UC-01 — Submit and decide simulated KYC

**Primary actor:** Customer; Operations user for the decision.

**Preconditions:** The customer is authenticated and has no active pending submission.

**Successful flow:**

1. The customer submits bounded fictional onboarding data.
2. The system validates shape and state.
3. The system records a pending submission and audit information.
4. An authorized operations user reviews the submission.
5. The operations user approves or rejects it with a reason.
6. The system records the decision and makes the resulting customer status visible.

**Postconditions:** Exactly one auditable decision exists for the submission. Credentials or sensitive documents are not logged.

### UC-02 — Open a wallet

**Primary actor:** Customer.

**Preconditions:** The customer is authenticated, KYC-approved, active, and does not already own an active wallet in the requested currency.

**Successful flow:**

1. The customer selects a supported currency.
2. The system validates ownership, KYC, currency, and uniqueness rules.
3. The Accounts module creates the wallet.
4. The Ledger module creates or maps the corresponding ledger account through an explicit contract.
5. The system returns the wallet identifier and zero balances.

**Postconditions:** The wallet is active, owned by the customer, mapped to accounting truth, and auditable.

### UC-03 — Add sandbox funds

**Primary actor:** Operations user.

**Preconditions:** The operator is authenticated and authorized; the destination wallet is active; the request has an idempotency key.

**Successful flow:**

1. The operator submits wallet, amount, currency, reason, and idempotency key.
2. The system validates the operator, wallet, amount, currency, and request-key history.
3. The ledger posts a balanced entry between sandbox clearing and the customer wallet account.
4. The operation, idempotency result, audit data, and outbox message commit atomically.
5. The system returns the stable result.

**Postconditions:** The wallet has one additional ledger credit and the operation can be safely retried.

### UC-04 — Transfer funds between customers

**Primary actor:** Customer.

**Preconditions:**

- the customer is authenticated, active, and KYC-approved;
- the source wallet belongs to the customer and is active;
- the destination wallet exists and is active;
- both wallets use the same supported currency;
- the amount is positive and within configured limits;
- the source has sufficient available funds;
- the request has an idempotency key.

**Successful flow:**

1. The customer submits source wallet, destination wallet, amount, currency, optional bounded reference, and idempotency key.
2. The API validates transport shape and authenticates the caller.
3. Payments loads or claims the idempotency record.
4. Payments verifies resource ownership and asks Accounts/Risk for required decisions.
5. Ledger verifies the spendable position under the selected concurrency strategy.
6. Ledger creates one balanced journal entry with source and destination postings.
7. Payments records the completed transfer and response.
8. The transfer, journal entry, postings, idempotency result, audit record, and outbox message commit in one PostgreSQL transaction.
9. The API returns the stable transfer result.
10. The worker later sends fictional notifications from the outbox event.

**Postconditions:**

- booked funds moved exactly once;
- debits equal credits for the journal entry;
- neither wallet is overdrawn;
- retrying the identical request returns the same result;
- the original committed operation is traceable and immutable;
- notification failure cannot roll back or duplicate the transfer.

### UC-05 — Read an account statement

**Primary actor:** Customer.

**Preconditions:** The customer owns the requested wallet.

**Successful flow:**

1. The customer supplies a wallet and optional cursor/page size.
2. The system authorizes resource ownership before disclosing existence or data.
3. Reporting reads a stable projection ordered by a documented unique cursor.
4. The API returns bounded items and a continuation cursor when more data exists.

**Postconditions:** No financial write occurs. The result can be reconciled with ledger postings.

### UC-06 — Reverse a transfer

**Primary actor:** Operations user.

**Preconditions:** The operator is authorized; the original transfer is completed, eligible, and not already reversed; the original recipient can return the full amount without overdraft; a bounded reason and idempotency key are supplied.

**Successful flow:**

1. The operator requests full reversal with a reason.
2. The system validates authority, eligibility, current state, and request identity.
3. Ledger creates a new balanced compensating entry referencing the original entry.
4. Payments records the reversal relationship and resulting transfer state.
5. Financial changes, idempotency result, audit data, and outbox event commit atomically.

**Postconditions:** The original history remains unchanged; one compensating entry exists; repeated reversal cannot move funds again.

## 9. Business rules and invariants

| ID | Rule |
|---|---|
| BR-MNY-001 | Never use binary floating-point types for money. |
| BR-MNY-002 | Money consists of integer minor units and one supported currency. |
| BR-MNY-003 | Currency is never inferred from locale, wallet owner, or display text. |
| BR-MNY-004 | Version 1 performs no currency conversion. |
| BR-ACC-001 | A wallet has one owner and one currency for its lifetime. |
| BR-ACC-002 | A customer has at most one active wallet per supported currency. |
| BR-ACC-003 | Closed wallets cannot be reopened in Version 1. |
| BR-LED-001 | A posted journal entry contains at least two postings. |
| BR-LED-002 | Total debits equal total credits per journal entry and currency. |
| BR-LED-003 | Posted journal entries and postings are immutable. |
| BR-LED-004 | Corrections and reversals create new entries that reference the original. |
| BR-LED-005 | Ledger-derived balance is the financial source of truth. |
| BR-LED-006 | Every financial posting has a unique business-operation reference. |
| BR-TRF-001 | Transfer amount is strictly positive. |
| BR-TRF-002 | Source and destination wallets are different and use the same currency. |
| BR-TRF-003 | The caller must own the source wallet unless a separately authorized operation is modeled. |
| BR-TRF-004 | Source and destination wallets must be active when posting begins. |
| BR-TRF-005 | Available funds cannot fall below zero. |
| BR-TRF-006 | A transfer is completed only if its financial posting commits. |
| BR-IDM-001 | The same idempotency key and normalized request return the original result. |
| BR-IDM-002 | The same key with a different normalized request is rejected. |
| BR-IDM-003 | Idempotency storage follows an explicit retention/expiry policy. |
| BR-REV-001 | A completed transfer can have at most one successful full reversal. |
| BR-REV-002 | A reversal requires an authorized actor and non-empty bounded reason. |
| BR-REV-003 | A Version 1 reversal cannot make the original recipient's available balance negative. |
| BR-AUD-001 | Important state changes record actor, time, correlation, and reason/context. |
| BR-TIM-001 | Business instants are handled as UTC and time-dependent behavior uses a testable clock. |

## 10. Failure and abuse cases

| ID | Scenario | Required outcome |
|---|---|---|
| FC-001 | Missing or malformed authentication | Reject without financial work. |
| FC-002 | Customer requests another customer's wallet | Deny without leaking sensitive resource details. |
| FC-003 | Non-operator requests funding or reversal | Deny and record appropriate security telemetry. |
| FC-004 | Zero or negative amount | Reject before persistence. |
| FC-005 | Unsupported or mismatched currency | Reject; do not convert implicitly. |
| FC-006 | Same wallet used as source and destination | Reject. |
| FC-007 | Source or destination is suspended/closed | Reject according to documented state rules. |
| FC-008 | Insufficient available funds | Reject with no partial ledger entry. |
| FC-009 | Duplicate request arrives sequentially | Return stored result; do not post again. |
| FC-010 | Duplicate request arrives concurrently | One request wins; all equivalent requests observe one result. |
| FC-011 | Idempotency key reused for a different payload | Reject as a conflict. |
| FC-012 | Two different transfers race to spend the same funds | At most the affordable operations complete; no overdraft. |
| FC-013 | SQL command fails before commit | Roll back all parts of the financial operation. |
| FC-014 | API loses connection after commit | Retry returns the committed result through idempotency. |
| FC-015 | Outbox worker is unavailable | Financial commit remains valid; notification waits and is observable. |
| FC-016 | Worker crashes after side effect but before marking completion | Retry is harmless through consumer/adapter deduplication. |
| FC-017 | Reversal is requested twice | One reversal succeeds; the duplicate cannot move money. |
| FC-017A | Original recipient lacks funds for full reversal | Reject as ineligible; do not overdraw or partially reverse. |
| FC-018 | Tampered cursor or identifier | Reject safely; disclose no unauthorized data. |
| FC-019 | Oversized text/page/request body | Reject at the boundary with bounded resource cost. |
| FC-020 | Log/trace exporter fails | Core operation continues according to an explicit telemetry failure policy. |
| FC-021 | Database is unavailable | Readiness fails; financial request fails safely; no success is reported. |
| FC-022 | Reconciliation detects imbalance or broken reference | Raise a high-severity operational signal and stop unsafe automated correction. |

## 11. Version 1 non-goals

- real financial transactions or real customer onboarding;
- legal, regulatory, tax, KYC, AML, sanctions, or PCI compliance claims;
- cardholder data, bank credentials, identity documents, or biometrics;
- external bank/card settlement, chargebacks, disputes, cash withdrawal, or merchant acquiring;
- lending, interest, investments, cryptocurrency, rewards, fees, and foreign exchange;
- joint/business accounts and multiple approval workflows;
- partial reversals;
- multi-region active-active operation;
- Kubernetes, service mesh, distributed database, or event sourcing;
- independently deployed microservices in the initial architecture;
- a feature-complete customer or operations frontend.

## 12. Release acceptance criteria

Version 1 can be called a **public learning release** only when:

1. All Version 1 requirements are implemented or explicitly moved out of scope.
2. All listed financial invariants have automated tests and applicable database protection.
3. Duplicate and concurrent transfer tests demonstrate exactly one financial effect and no overdraft.
4. Reconciliation reports zero unexplained imbalance in the release dataset.
5. Resource-owner and operator authorization tests pass.
6. A clean clone can build, test, migrate, seed fictional data, and run from public instructions.
7. CI verifies formatting, build, tests, and selected security/dependency checks.
8. Logs, metrics, traces, health checks, and one incident exercise are documented.
9. Deployment, migration, backup/restore, smoke-test, and rollback procedures have been exercised.
10. Known limitations and security gaps are published honestly.
11. No real secrets, personal data, or regulated data exist in the repository or demo.
12. The repository owner can explain and modify the core transfer path without AI assistance.

## 13. Product risks

| Risk | Consequence | Treatment |
|---|---|---|
| Scope grows into an imaginary bank | Project never reaches a credible release | Enforce Version 1 non-goals and phase gates. |
| Displayed balance becomes source of truth | Silent inconsistency or money creation | Keep ledger authoritative and reconcile projections. |
| Architecture has too many abstractions | Learning is replaced by template maintenance | Require a named problem for every abstraction/package. |
| Authentication is mistaken for authorization | Cross-customer data or money access | Resource-level authorization and negative tests. |
| Retry logic duplicates money movement | Financial loss in a real analogue | Idempotency, unique constraints, atomic storage, concurrency tests. |
| Async messaging enters the financial commit too early | Partial failure and hard debugging | Commit core financial truth synchronously; publish through outbox. |
| Public demo attracts abuse or cost | Outage or unexpected cloud expense | Fictional data, quotas, rate limits, restricted operations, budgets. |
| Documentation becomes stale | Reviewers learn the wrong system | Treat docs and ADRs as Definition-of-Done artifacts. |

## 14. Decisions intentionally deferred

These require evidence during their roadmap phase:

- exact per-currency transfer and daily limits;
- final authentication provider and token/session design;
- whether pending/held funds are needed before any external-payment experiment;
- exact PostgreSQL isolation/concurrency strategy for the transfer posting path;
- idempotency retention duration and cleanup mechanism;
- statement performance target after a measured baseline;
- message broker selection, if any;
- cloud hosting target and cost budget;
- frontend framework;
- whether any module should ever be independently deployed.

Deferred does not mean ignored. Each item must be resolved in an ADR before implementation depends on it.

## 15. Change control

A change to money representation, ledger invariants, transfer atomicity, idempotency semantics, authorization ownership, reversal rules, or module data ownership requires:

1. an updated requirement/rule;
2. a new or superseding ADR;
3. updated diagrams;
4. updated threat and failure analysis;
5. updated tests and migration plan;
6. review before merge.
