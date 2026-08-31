# Quality Attributes and Engineering Fitness Functions

## 1. Purpose

This document turns “professional,” “secure,” “maintainable,” and “scalable” into reviewable expectations. Targets are learning objectives for a documented sandbox environment, not production SLAs or regulatory guarantees.

Priorities:

- **P0:** failure invalidates the financial/security model;
- **P1:** required for a credible public backend release;
- **P2:** important evolution target after the core is correct.

## 2. Priority order

When qualities conflict, use this default order:

1. financial correctness;
2. security and privacy;
3. auditability and recoverability;
4. reliability;
5. maintainability and testability;
6. observability and operability;
7. performance and scalability;
8. delivery speed and convenience.

This order may be changed only for a specific decision with documented consequences. Performance never justifies silently weakening a financial invariant.

## 3. Quality scenarios

### QA-COR-001 — Balanced financial posting (P0)

| Element | Definition |
|---|---|
| Source | Any accepted money-moving command |
| Stimulus | The system attempts to post financial entries |
| Environment | Normal operation, retry, or injected persistence failure |
| Artifact | Journal entry and postings |
| Response | Commit one balanced immutable entry or commit no financial entry |
| Measure | Automated tests and reconciliation find zero unbalanced committed journal entries |

### QA-COR-002 — No overspending under concurrency (P0)

| Element | Definition |
|---|---|
| Source | Multiple authenticated clients |
| Stimulus | Concurrent transfers collectively exceed one source wallet's funds |
| Environment | Real PostgreSQL 18 integration test through Npgsql |
| Artifact | Wallet/ledger spend position and transfer records |
| Response | Only affordable operations complete; conflicts fail deterministically |
| Measure | No negative available balance and no unexplained posting after repeated stress runs |

### QA-COR-003 — Idempotent money movement (P0)

| Element | Definition |
|---|---|
| Source | Client, proxy, or retry policy |
| Stimulus | Same normalized request and idempotency key are submitted repeatedly or concurrently |
| Environment | Before, during, and after the original commit |
| Artifact | Transfer/funding/reversal operation |
| Response | One business effect and one stable outcome |
| Measure | Repeating the request at least 100 times creates exactly one financial operation in the test database |

### QA-SEC-001 — Resource isolation (P0)

| Element | Definition |
|---|---|
| Source | Authenticated Customer A |
| Stimulus | Attempts to read or operate Customer B's resource identifier |
| Environment | API and background paths |
| Artifact | Customer, wallet, transfer, and statement resources |
| Response | Access is denied without sensitive disclosure or side effect |
| Measure | Negative functional tests cover every customer-resource endpoint |

### QA-SEC-002 — Sensitive-data minimization (P0)

| Element | Definition |
|---|---|
| Source | Application, dependency, exception, or operator action |
| Stimulus | Logs, traces, metrics, errors, health output, or examples are emitted |
| Environment | Development and deployed environments |
| Artifact | Observability and API outputs |
| Response | Secrets and prohibited sensitive data are excluded or safely redacted |
| Measure | Automated redaction tests plus release review find no prohibited values |

### QA-AUD-001 — Financial traceability (P0)

| Element | Definition |
|---|---|
| Source | Support or incident investigation |
| Stimulus | Investigator receives a transfer, journal-entry, or correlation identifier |
| Environment | Retained sandbox data |
| Artifact | Business records, ledger, audit data, and telemetry |
| Response | Investigator can connect actor, request, financial posting, outbox event, and result |
| Measure | Incident exercise reconstructs the selected operation without direct data editing |

### QA-REL-001 — Transactional failure safety (P0)

| Element | Definition |
|---|---|
| Source | Database, application cancellation, or injected exception |
| Stimulus | Failure occurs at each planned point in a money-moving workflow |
| Environment | Integration test with real PostgreSQL 18 |
| Artifact | Transfer, ledger, idempotency, audit, and outbox data |
| Response | All required state commits or all rolls back; no success is reported for rolled-back work |
| Measure | Failure-injection test verifies every transaction checkpoint |

### QA-REL-002 — Asynchronous recovery (P1)

| Element | Definition |
|---|---|
| Source | Worker/process failure |
| Stimulus | Worker crashes before, during, or after message handling |
| Environment | Outbox backlog exists |
| Artifact | Outbox message and fake notification effect |
| Response | Processing resumes; no message is lost; duplicate attempt is harmless |
| Measure | Restart exercise drains backlog and produces one observable logical notification |

### QA-MNT-001 — Enforced module boundaries (P1)

| Element | Definition |
|---|---|
| Source | Developer change |
| Stimulus | Code introduces a forbidden module/project dependency or cross-module table access |
| Environment | Local build and CI |
| Artifact | Solution dependency graph and persistence code |
| Response | Architecture test/build/review blocks the change |
| Measure | Zero cyclic module references; automated fitness tests cover declared dependency rules |

### QA-MNT-002 — Safe changeability (P1)

| Element | Definition |
|---|---|
| Source | Developer |
| Stimulus | Adds a new statement field or notification channel |
| Environment | Existing Version 1 system |
| Artifact | Relevant module and contracts |
| Response | Change remains localized and preserves unrelated modules/contracts |
| Measure | Pull request touches only justified modules; all regression tests remain green |

### QA-TST-001 — Reproducible verification (P1)

| Element | Definition |
|---|---|
| Source | New contributor or CI runner |
| Stimulus | Clean clone executes documented verification |
| Environment | Supported local/CI platform with declared prerequisites |
| Artifact | Source, database migration, and test suite |
| Response | Restore, build, database setup, and tests run without private instructions |
| Measure | Clean-environment rehearsal succeeds using repository documentation only |

### QA-OBS-001 — End-to-end diagnosis (P1)

| Element | Definition |
|---|---|
| Source | Operator |
| Stimulus | Receives a failed or slow transfer report and a correlation/trace identifier |
| Environment | Instrumented local or staging environment |
| Artifact | API, SQL, outbox, and worker activity |
| Response | Operator identifies failing stage and relevant business identifier without sensitive data |
| Measure | Timed incident exercise reaches a supported root-cause hypothesis within 15 minutes |

### QA-PER-001 — Transfer latency baseline (P2)

| Element | Definition |
|---|---|
| Source | Authenticated sandbox clients |
| Stimulus | Valid internal transfers at a documented load |
| Environment | Declared hardware, dataset size, and database configuration |
| Artifact | API and transfer write path |
| Response | Requests complete without invariant violations or resource exhaustion |
| Measure | Establish baseline first; then target p95 under 500 ms at 25 requests/second for the documented lab profile |

The numeric target is a learning target, not a general production promise. Change it only with a new measured baseline and documented environment.

### QA-SCL-001 — Stateless API scale-out readiness (P2)

| Element | Definition |
|---|---|
| Source | Increased request volume |
| Stimulus | A second API instance handles traffic |
| Environment | Shared PostgreSQL and shared cryptographic key strategy where required |
| Artifact | API runtime state |
| Response | Correctness does not depend on in-process session, locks, or cache |
| Measure | Multi-instance test preserves idempotency, authorization, and transaction behavior |

### QA-OPS-001 — Recoverable deployment (P1)

| Element | Definition |
|---|---|
| Source | Release operator |
| Stimulus | Deploys a tagged version or detects a failed release |
| Environment | Staging/public sandbox |
| Artifact | Application, configuration, and schema |
| Response | Smoke test verifies release or documented rollback/forward-fix restores service |
| Measure | Rehearsal records commands, evidence, duration, and schema compatibility result |

### QA-REC-001 — Backup and restore exercise (P1)

| Element | Definition |
|---|---|
| Source | Operator |
| Stimulus | Sandbox database becomes unavailable or corrupted in an exercise |
| Environment | Staging-like environment |
| Artifact | PostgreSQL data, required role/grant definitions, and deployment configuration |
| Response | Restore from a known backup, reconcile, and resume safely |
| Measure | Initial learning target: RPO at most 15 minutes and RTO at most 60 minutes for the documented exercise |

These RPO/RTO values are lab goals and must not be advertised as a customer commitment.

For PostgreSQL, record whether the exercise uses logical backup/restore or a base-backup/WAL/managed point-in-time recovery process. A one-off `pg_dump` is not evidence of a continuous 15-minute RPO. Restore and reconcile all required schemas, roles, and grants before resuming traffic.

## 4. Architecture fitness functions

Automate these checks as the implementation matures:

| Fitness function | Evidence |
|---|---|
| Domain projects do not reference ASP.NET Core, EF Core, or provider packages | Project/architecture test |
| No cyclic module dependencies | Architecture test/dependency graph |
| Posted ledger records have no supported update/delete path | Domain tests, persistence tests, code review |
| Every money-moving endpoint requires idempotency | Endpoint metadata/functional tests |
| Customer resource endpoints enforce ownership | Negative functional test matrix |
| Npgsql migrations rebuild all module schemas with separate history locations | PostgreSQL 18 CI integration job |
| UTC instants and signed minor units round-trip without unintended conversion | Npgsql `timestamptz`/`bigint` integration tests |
| Partial uniqueness and ownership rules survive concurrent lifecycle changes | PostgreSQL wallet creation/reactivation tests |
| Schema ownership, runtime grants, and trusted search path match the documented policy | Role/configuration tests |
| Core queries have bounded page size | Contract and functional tests |
| Logs do not contain seeded secret markers | Automated log-capture test |
| OpenAPI generation succeeds and expected operations exist | Contract test |
| Reconciliation passes after concurrency tests | Integration test |
| Release build has no compiler warnings | CI gate |
| Documentation links and Mermaid fences remain valid | Documentation verification script/check |

## 5. Review policy

- P0 scenarios must have evidence before the relevant feature is considered complete.
- P1 scenarios must have evidence before a public Version 1 release.
- P2 scenarios begin after a measured baseline; they must not drive premature infrastructure.
- A target may be revised when the environment and reason are documented.
- A passing metric does not override a failed financial or security invariant.
