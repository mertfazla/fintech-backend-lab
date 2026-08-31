# Security Architecture and Threat Model

## 1. Scope and posture

This document defines security expectations for a public educational sandbox. It improves engineering quality but does not claim regulatory compliance, formal certification, penetration testing, or fitness for real money or personal data.

Security is treated as a continuous property of product requirements, architecture, implementation, tests, deployment, and operations.

## 2. Security objectives

1. Prevent unauthorized access to another customer's resources.
2. Prevent unauthorized money-moving and privileged operations.
3. Preserve ledger integrity and transaction atomicity.
4. Prevent duplicate or conflicting requests from creating multiple financial effects.
5. Minimize collected/stored data and prohibit real sensitive data.
6. Avoid leaking secrets or resource details through APIs, logs, traces, metrics, health checks, or documentation.
7. Keep dependencies, configuration, and deployment identities least-privileged and reviewable.
8. Make suspicious and failed activity diagnosable without logging sensitive content.
9. Recover safely from dependency, process, and deployment failures.

## 3. Security non-goals

- certification against PCI DSS, ISO 27001, SOC 2, or another standard;
- legal compliance with any jurisdiction's financial/privacy regulation;
- protection of real customer/card/bank/identity data;
- custom cryptographic protocol, token format, or identity provider;
- a guarantee of complete security or resistance to unlimited denial-of-service attacks;
- production fraud detection or AML/sanctions decisions.

## 4. Protected assets

| Asset | Why it matters | Classification in this sandbox |
|---|---|---|
| Authentication credentials/tokens | Grant user or operator capabilities | Secret; never seed, log, or commit real values |
| Deployment/database credentials | Grant infrastructure capabilities | Secret; external secret store only |
| Customer identity mapping | Connects an authenticated identity to a customer | Confidential fictional data |
| Wallet ownership/status | Controls who may view/spend | Confidential integrity-critical data |
| Transfer and idempotency records | Determine financial effects and safe retries | Confidential integrity-critical data |
| Journal entries/postings | Financial source of truth | Highly integrity-critical fictional data |
| Audit records | Support accountability and investigation | Integrity-critical, access-restricted |
| Outbox/inbox records | Support reliable downstream effects | Integrity-critical operational data |
| Logs/traces/metrics | Can disclose identifiers and operation behavior | Internal operational data, minimized |
| Source and CI configuration | Can introduce vulnerabilities or leak secrets | Public source except protected settings/secrets |

## 5. Trust boundaries

### TB-01 — Internet client to public ingress/API

All input is untrusted, including headers, identifiers, cursors, JSON, forwarded network data, and idempotency keys. HTTPS, authentication, request bounds, rate limits, validation, and authorization apply.

### TB-02 — Authenticated identity to application authorization

A valid identity proves who the caller is, not what they may access. Claims are mapped to internal caller context and checked against policy and resource ownership.

### TB-03 — API/worker to PostgreSQL

Database access uses EF Core/Npgsql with parameterized commands, bounded timeouts, explicit transactions, and least-privileged roles. Deployed connections require TLS with server-certificate and hostname validation. Separate migration/owner roles from runtime roles; never run the API or worker as a PostgreSQL superuser or schema owner.

Map schemas explicitly and review schema `USAGE`, object grants, and `search_path`. Do not trust schemas writable by untrusted users. A shared runtime role can access multiple granted schemas, so module ownership still needs code boundaries and tests; schemas alone do not isolate modules. See [PostgreSQL schemas and privileges](https://www.postgresql.org/docs/18/ddl-schemas.html).

### TB-04 — Financial commit to asynchronous worker

Only committed outbox records cross this boundary. Consumers assume duplicates and validate event type/version/payload bounds.

### TB-05 — Runtime to telemetry backend

Telemetry is an information-disclosure path. Exported attributes are allow-listed/minimized and never include credentials, tokens, full request bodies, or prohibited personal data.

### TB-06 — CI/CD to deployment environment

Workflow definitions and dependencies are untrusted change surfaces. Protected environments, least permissions, reviewed workflows, pinned/reviewed actions, and short-lived/federated credentials are preferred.

## 6. Identity design

### 6.1 Authentication

- Use ASP.NET Core-supported, standards-based authentication.
- Do not build password hashing, MFA, session, token issuance, refresh, or revocation protocols from scratch.
- Development authentication must be clearly limited and replaceable by an external OpenID Connect/OAuth-based provider.
- Validate issuer, audience, signature, lifetime, and intended authentication scheme according to the selected provider/flow.
- Do not place access tokens in URLs, source, documentation examples, logs, or Scalar/OpenAPI configuration.
- Protect any persistent ASP.NET Core Data Protection key material appropriate to hosting topology.

The final provider and flow require an ADR during the identity phase.

### 6.2 Authorization

Authorization has three layers:

1. **Endpoint policy:** is this capability allowed for Customer, Operations, or Worker identity?
2. **Resource ownership:** does this customer own or legitimately participate in this wallet/transfer/statement?
3. **Domain eligibility:** is the operation valid for the current KYC, wallet, transfer, and limit state?

Do not treat domain eligibility as a substitute for security authorization, or vice versa.

### 6.3 Roles and capabilities

Prefer policies/capabilities over scattered string-role checks. Proposed capabilities include:

- `CustomerProfileReadOwn`
- `WalletOpenOwn`
- `WalletReadOwn`
- `TransferCreateOwn`
- `TransferReadOwn`
- `KycReview`
- `SandboxFundingCreate`
- `TransferReverse`
- `AuditRead`
- `OutboxProcess`

Names are conceptual until the identity design is implemented.

## 7. Authorization matrix

| Capability/resource | Customer | Operations user | Worker |
|---|---:|---:|---:|
| Read own profile/wallet/statement | Allow with ownership | Allow only for documented support purpose | Deny |
| Read another customer's resources | Deny | Conditional, least-privileged, audited | Deny |
| Open own wallet | Allow when KYC-eligible | Deny by default | Deny |
| Create transfer from own wallet | Allow with ownership/domain checks | Deny by default; separate operation if ever needed | Deny |
| Decide simulated KYC | Deny | Allow with policy and audit | Deny |
| Add sandbox funds | Deny | Allow with policy, idempotency, reason, audit | Deny |
| Reverse eligible transfer | Deny | Allow with policy, idempotency, reason, audit | Deny |
| Edit/delete posted ledger history | Deny | Deny | Deny |
| Process outbox | Deny | Deny | Allow only through worker identity |
| Read detailed audit/diagnostics | Deny | Conditional separate policy | Conditional operational access only |

Every allow decision is deny-by-default and still subject to input, state, and invariant validation.

## 8. API security controls

### 8.1 Input handling

- Bind to dedicated request contracts, not persistence entities.
- Allow-list writable properties to prevent mass assignment.
- Bound text length, collection count, page size, numeric range, and request-body size.
- Validate identifiers, currency, amount, state transitions, and idempotency-key format/length.
- Use `System.Text.Json` defaults/configuration deliberately; reject ambiguous or unsupported shapes.
- Treat URLs and future external callbacks as SSRF risk; Version 1 accepts no arbitrary outbound URL.
- Use parameterized EF Core/LINQ and reviewed raw SQL only when justified.

### 8.2 Output handling

- Return only fields required by the caller.
- Never serialize domain/persistence entities directly.
- Use stable Problem Details codes without stack traces or SQL/provider messages.
- Decide safe `403` versus `404` behavior for protected resource discovery and test it consistently.
- Keep OpenAPI examples fictional and secret-free.
- Restrict runtime OpenAPI/Scalar to Development by default as defined in the working agreement.

### 8.3 HTTP controls

- HTTPS only in deployed environments, with trusted proxy/forwarded-header configuration.
- Explicit CORS allow-list only when a real browser frontend exists; no permissive wildcard with credentials.
- CSRF protection is required if cookie-authenticated state-changing browser flows are introduced.
- Endpoint-specific rate limits partitioned by authenticated identity and/or safe network key.
- Return `429` and useful bounded retry guidance where appropriate.
- Set timeouts and request limits according to endpoint cost.
- Do not trust client-supplied forwarded IP/host headers unless validated by known infrastructure.

## 9. Financial integrity controls

Financial correctness is a security boundary.

- Balanced journal-entry invariant.
- Immutable posted entries.
- Atomic operation/ledger/idempotency/audit/outbox transaction.
- Unique constraints for idempotency and business references.
- Concurrency strategy proving no overdraft.
- Full reversal through one compensating entry.
- Reconciliation independent from normal command behavior.
- No privileged direct balance-edit endpoint.
- No notification/provider outcome can redefine a committed internal transfer.
- Database runtime account receives only required data permissions.

## 10. Secrets and cryptography

- Local secrets use .NET user secrets or environment-specific secure tooling, never tracked JSON or `.env` files.
- Deployed secrets use the platform secret store or managed identity/federated access where possible.
- Rotate secrets and revoke compromised credentials through a documented procedure.
- Use platform TLS and established cryptographic APIs.
- Use ASP.NET Core Data Protection for its intended protected-state scenarios and manage keys for the deployment topology.
- Never create custom encryption, hashing, signing, token, or key-derivation algorithms.
- Encryption does not remove authorization, minimization, retention, or compliance concerns.
- Card data remains prohibited even if someone proposes encrypting it.

## 11. Logging, audit, and privacy

### 11.1 Never log

- passwords, access/refresh tokens, cookies, API keys, or connection strings;
- raw authorization headers;
- real or simulated identity-document content;
- full request/response bodies for authentication or financial commands;
- unnecessarily precise personal fields;
- exception objects if provider messages may contain sensitive SQL/data, without controlled handling.

### 11.2 Safe operational context

- trace/correlation identifier;
- opaque customer/wallet/transfer/journal identifiers when needed;
- operation name and outcome category;
- policy/authorization result category without leaking sensitive claims;
- duration, retry count, concurrency conflict, and dependency stage;
- error code rather than raw confidential message.

### 11.3 Audit versus application log

Audit data is durable business/security evidence with defined actors and actions. Application logs are diagnostic streams. Do not rely on transient logs as the only record of a privileged funding/reversal decision, and do not put secrets into an audit record.

## 12. Dependency and supply-chain security

- Prefer the framework/standard library where it meets requirements.
- For every dependency review maintenance, license, transitive dependencies, security history, and removal cost.
- Pin SDK and package versions through governed files and locked restore where adopted.
- Enable dependency/vulnerability alerts and secret scanning.
- Review automated dependency updates through build/tests rather than auto-merging blindly.
- Keep CI workflow permissions minimal.
- Review third-party GitHub Actions and pin/review references according to repository policy.
- Produce an inventory/SBOM if supported in the release workflow.

## 13. Threat analysis

| ID | Threat | Example attack/failure | Primary controls | Verification |
|---|---|---|---|---|
| TH-001 | Identity spoofing | Forged/invalid token | Standards-based validation, HTTPS, scheme configuration | Authentication negative tests |
| TH-002 | Broken object authorization | Customer changes wallet ID | Ownership policy, scoped queries, safe errors | Cross-customer endpoint matrix |
| TH-003 | Privilege escalation | Customer calls reversal/funding | Capability policy, deny-by-default, audit | Role/policy negative tests |
| TH-004 | Mass assignment | Request sets owner/status/internal fields | Dedicated request DTOs and allow-list mapping | Contract tests |
| TH-005 | Duplicate financial effect | Retry posts transfer twice | Idempotency record, unique constraint, atomic result | Sequential/concurrent duplicate tests |
| TH-006 | Race-condition overspend | Concurrent transfers spend same funds | Proven PostgreSQL concurrency strategy, not `xmin` alone | Real-PostgreSQL concurrency stress test |
| TH-007 | Ledger tampering | Update/delete posted entry | No supported mutation path, DB permissions, audit/reconciliation | Architecture/integration tests |
| TH-008 | Injection | Crafted input alters query | EF parameterization, bounded input, raw SQL review | Static/dynamic tests and review |
| TH-009 | Sensitive data exposure | Token/body appears in logs | Minimized structured logging and redaction | Marker-based log tests |
| TH-010 | Secret leakage | Credential committed to Git | Secret scanning, external stores, rotation plan | CI/repository scan |
| TH-011 | Resource exhaustion | Large pages/bodies or expensive requests | Bounds, timeouts, pagination, rate limits | Abuse/load tests |
| TH-012 | SSRF | User supplies callback URL | No arbitrary outbound URLs; future allow-list/egress policy | Contract and adapter tests |
| TH-013 | Unsafe message deserialization | Forged/oversized event | Versioned allow-listed contracts and bounded payload | Consumer negative tests |
| TH-014 | Outbox duplicate side effect | Worker crashes after send | Consumer/adapter deduplication | Crash/restart exercise |
| TH-015 | Dependency compromise | Malicious/vulnerable package/action | Minimal dependencies, alerts, review, pinning | CI and release review |
| TH-016 | Misconfiguration | Dev docs or detailed health public | Environment guards and deployment tests | Staging smoke/security test |
| TH-017 | Database privilege abuse | Runtime account changes schema/history | Least privilege, separate migration path | Permission test/review |
| TH-018 | Repudiation | Operator denies reversal | Durable actor/reason/time/correlation audit | Audit reconstruction exercise |
| TH-019 | Schema/search-path abuse | Untrusted schema object shadows an intended object | Explicit mappings, qualified raw SQL, restricted `CREATE`, trusted `search_path` | Role/grant and object-resolution tests |

## 14. OWASP API risk mapping

| OWASP API Security 2023 area | Project response |
|---|---|
| Broken Object Level Authorization | Resource ownership policies and negative tests for every customer resource endpoint |
| Broken Authentication | Framework/provider authentication, secure configuration, no custom protocol |
| Broken Object Property Level Authorization | Dedicated contracts and allow-listed mapping/output fields |
| Unrestricted Resource Consumption | Page/body/text bounds, endpoint-cost rate limits, timeouts, load tests |
| Broken Function Level Authorization | Separate customer/operator/worker capabilities and deny-by-default policies |
| Unrestricted Access to Sensitive Business Flows | Idempotency, limits, rate limits, audit, fictional funding restrictions |
| Server-Side Request Forgery | No arbitrary outbound URL in Version 1; explicit adapter destinations later |
| Security Misconfiguration | Environment validation, HTTPS, restricted docs/health, no default secrets |
| Improper Inventory Management | Versioned API/OpenAPI source of truth, documented endpoints and environments |
| Unsafe Consumption of APIs | Future adapters validate responses, apply timeouts/retries, and distrust provider data |

## 15. Security testing plan

- authentication scheme tests;
- `401`, `403`, and safe not-found behavior tests;
- cross-customer object access matrix;
- operator capability matrix;
- over-posting/mass-assignment tests;
- malformed, boundary, oversized, and unsupported input tests;
- idempotency and concurrency financial tests;
- PostgreSQL/Npgsql integration tests for unique/check/partial-index/permission behavior;
- UTC timestamp round-trip and PostgreSQL schema/search-path configuration tests;
- log/trace redaction tests with recognizable secret markers;
- rate-limit behavior tests;
- dependency, secret, and static analysis in CI;
- staging configuration smoke tests;
- manual threat-model review before public release;
- documented security incident exercise.

Automated scanners supplement reasoning; they do not prove the system secure.

## 16. Incident response outline

1. Detect and assign severity.
2. Preserve identifiers, logs, traces, database evidence, and affected version.
3. Contain access or affected deployment without destroying evidence.
4. Determine whether financial integrity, confidentiality, or availability was affected.
5. Reconcile relevant ledger/operation ranges.
6. Rotate/revoke credentials if exposure is possible.
7. Correct through tested deployment and compensating/rebuild procedures, never ad-hoc ledger edits.
8. Verify recovery and monitor recurrence.
9. Write a blameless postmortem with cause, contributing conditions, detection gaps, and actions.

## 17. Public-release security gate

- [ ] Only fictional data exists.
- [ ] No secret exists in current files or Git history.
- [ ] Authentication configuration has negative tests.
- [ ] Every protected resource endpoint has ownership/role tests.
- [ ] Money-moving endpoints prove idempotency and concurrency behavior.
- [ ] OpenAPI/Scalar and detailed health output are not exposed unintentionally.
- [ ] CORS/CSRF behavior matches the actual client/authentication design.
- [ ] Rate and resource limits are tested.
- [ ] Logs/traces pass redaction tests.
- [ ] Runtime database permissions are documented and reviewed.
- [ ] Dependency/security scans run in CI and findings are triaged.
- [ ] Backup/restore, rollback, and incident exercises are documented.
- [ ] Known gaps and non-production status are visible.

## 18. References

- [OWASP API Security Top 10 — 2023](https://owasp.org/API-Security/editions/2023/en/0x11-t10/)
- [OWASP Application Security Verification Standard](https://owasp.org/www-project-application-security-verification-standard/)
- [ASP.NET Core 10 security topics](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0)
- [ASP.NET Core 10 authentication overview](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0)
- [ASP.NET Core 10 authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/introduction?view=aspnetcore-10.0)
- [ASP.NET Core 10 rate limiting](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [PCI Security Standards Council: PCI DSS](https://www.pcisecuritystandards.org/standards/pci-dss/)
- [PostgreSQL 18 schemas and privileges](https://www.postgresql.org/docs/18/ddl-schemas.html)
- [PostgreSQL 18 transaction isolation](https://www.postgresql.org/docs/18/transaction-iso.html)
