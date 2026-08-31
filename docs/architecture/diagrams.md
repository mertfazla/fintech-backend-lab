# Architecture Diagrams

These are Version 1 design views. They describe intended boundaries and flows; they do not claim that the application has been implemented or deployed.

All active database views target PostgreSQL 18 through EF Core 10/Npgsql 10.x; [ADR 0006](../adr/0006-use-postgresql-and-npgsql.md) records the engine change. Module schema names remain valid PostgreSQL namespaces.

## 1. System context

This view shows who uses the sandbox and which external technical systems surround it.

```mermaid
flowchart LR
    Customer[Customer<br/>fictional end user]
    Operator[Operations user<br/>fictional employee]
    Developer[Developer / reviewer]
    System[Fintech Backend Lab<br/>wallet, transfers, ledger, audit]
    IdP[Standards-based identity provider<br/>development or future external provider]
    Notify[Fake notification adapter]
    Telemetry[Telemetry backend]

    Customer -->|HTTPS API| System
    Operator -->|HTTPS API| System
    Developer -->|OpenAPI client and operational review| System
    System -->|Authenticate / validate identity| IdP
    System -->|Fictional notification| Notify
    System -->|Sanitized logs, metrics, traces| Telemetry
```

## 2. Initial runtime/container view

The modular monolith is one deployment boundary, with a worker added when asynchronous processing begins.

```mermaid
flowchart TB
    Client[OpenAPI client<br/>optional thin web UI]
    Ingress[HTTPS ingress]

    subgraph ApplicationBoundary[Application deployment boundary]
        API[ASP.NET Core 10 API<br/>controllers, auth, module composition]
        Worker[.NET Worker<br/>outbox processing]
    end

    DB[(PostgreSQL 18<br/>module-owned schemas)]
    IdP[Identity provider]
    Fake[Fake notification adapter]
    OTel[OpenTelemetry backend/dashboard]

    Client --> Ingress --> API
    API --> IdP
    API --> DB
    Worker --> DB
    Worker --> Fake
    API -. sanitized telemetry .-> OTel
    Worker -. sanitized telemetry .-> OTel
```

## 3. Module map and allowed business dependencies

Arrows mean “may depend on a public contract from.” Ledger deliberately does not depend on Payments.

```mermaid
flowchart LR
    API[API composition]
    IAM[Identity and Access]
    CUS[Customers]
    ACC[Accounts]
    PAY[Payments]
    LED[Ledger]
    RISK[Risk and Limits]
    REP[Reporting]
    NOTIF[Notifications]
    BB[Small domain-neutral<br/>building blocks]

    API --> IAM
    API --> CUS
    API --> ACC
    API --> PAY
    API --> REP

    ACC -->|eligibility contract| CUS
    PAY -->|wallet contract| ACC
    PAY -->|posting contract| LED
    PAY -->|decision contract| RISK
    REP -->|read contracts / projections| ACC
    REP -->|read contracts / projections| LED
    REP -->|read contracts / projections| PAY
    NOTIF -->|consumes committed events| PAY

    CUS --> BB
    ACC --> BB
    PAY --> BB
    LED --> BB
    RISK --> BB
```

Forbidden examples: Ledger to Payments, Domain to API, one module's Infrastructure to another module's Infrastructure, and cyclic references.

## 4. Internal module structure

Dependencies point inward. Transport and infrastructure are replaceable details around domain/application behavior.

```mermaid
flowchart TB
    Transport[Transport adapters<br/>controllers, request/response contracts]
    Features[Application / vertical slices<br/>use-case orchestration]
    Domain[Domain core<br/>entities, value objects, invariants, policies]
    Contracts[Module public contracts]
    Infra[Infrastructure<br/>EF Core and Npgsql, mappings, adapters]
    External[(PostgreSQL / external provider / queue)]

    Transport --> Features
    Features --> Domain
    Features --> Contracts
    Infra --> Domain
    Infra --> Contracts
    Infra --> External
    Transport -. composed with .-> Infra
```

## 5. ASP.NET Core request path

```mermaid
flowchart LR
    Request[HTTPS request]
    Safety[Forwarded headers / HTTPS / request limits]
    Trace[Correlation and tracing]
    Errors[Exception handling / Problem Details]
    AuthN[Authentication]
    Rate[Partitioned rate limit]
    AuthZ[Endpoint and resource authorization]
    Bind[Model binding and boundary validation]
    Handler[Module use-case handler]
    Domain[Domain rules]
    Persistence[Module persistence / PostgreSQL transaction]
    Response[Documented response]

    Request --> Safety --> Trace --> Errors --> AuthN --> Rate --> AuthZ --> Bind --> Handler --> Domain --> Persistence --> Response
```

The exact middleware order is verified against implementation behavior; this diagram captures responsibilities, not executable configuration.

## 6. Successful internal transfer sequence

```mermaid
sequenceDiagram
    autonumber
    actor Customer
    participant API as API boundary
    participant Payments
    participant Accounts
    participant Risk
    participant Ledger
    participant SQL as PostgreSQL transaction via Npgsql
    participant Worker
    participant Notify as Fake notification

    Customer->>API: POST transfer + Idempotency-Key
    API->>API: Authenticate, validate, authorize
    API->>Payments: Create transfer command
    Payments->>SQL: Claim/read idempotency record
    Payments->>Accounts: Verify ownership, currency, wallet states
    Accounts-->>Payments: Wallet decision data
    Payments->>Risk: Evaluate configured limits
    Risk-->>Payments: Allow or deny with reason
    Payments->>Ledger: Prepare balanced posting
    Ledger->>SQL: Verify spend position and stage journal/postings
    Payments->>SQL: Stage transfer, stable result, audit, outbox
    SQL-->>Payments: Atomic commit succeeds
    Payments-->>API: Stable completed transfer result
    API-->>Customer: Success response
    Worker->>SQL: Claim committed outbox message
    Worker->>Notify: Deliver fictional notification
    Notify-->>Worker: Delivery result
    Worker->>SQL: Mark processing result safely
```

## 7. Transfer transaction boundary

Only the solid box commits atomically. Response transmission and asynchronous effects remain outside.

```mermaid
flowchart TB
    Validate[Authenticate, authorize,<br/>validate bounded request]

    subgraph Tx[One short PostgreSQL transaction]
        Idem[Claim idempotency key<br/>and request fingerprint]
        Recheck[Re-check authoritative wallet,<br/>limit, and spend conditions]
        Transfer[Persist transfer state]
        Journal[Persist balanced journal entry<br/>and postings]
        Audit[Persist required audit metadata]
        Outbox[Persist integration event]
        Result[Persist stable idempotent result]

        Idem --> Recheck --> Transfer --> Journal --> Audit --> Outbox --> Result
    end

    Commit{Commit?}
    Return[Return stable API result]
    Async[Worker delivers notifications]
    Rollback[Rollback all staged changes<br/>and report no success]

    Validate --> Idem
    Result --> Commit
    Commit -->|yes| Return --> Async
    Commit -->|no| Rollback
```

## 8. Idempotency decision flow

```mermaid
flowchart TD
    Start[Money-moving request]
    Key{Valid idempotency key?}
    Existing{Key exists in caller<br/>and operation scope?}
    Match{Stored fingerprint matches?}
    Done{Stored operation completed?}
    Claim[Atomically claim key]
    Execute[Execute complete financial transaction]
    Store[Store stable result atomically]
    Original[Return original stable result]
    Conflict[Reject key reuse conflict]
    Pending[Return documented in-progress/conflict behavior]
    Reject[Reject request]

    Start --> Key
    Key -->|no| Reject
    Key -->|yes| Existing
    Existing -->|no| Claim --> Execute --> Store
    Existing -->|yes| Match
    Match -->|no| Conflict
    Match -->|yes| Done
    Done -->|yes| Original
    Done -->|no| Pending
```

## 9. Transfer lifecycle

The final implementation may refine names, but transitions must remain explicit and auditable.

```mermaid
stateDiagram-v2
    [*] --> Received
    Received --> Rejected: validation / authorization / idempotency conflict
    Received --> Processing: request claimed
    Processing --> Completed: financial transaction committed
    Processing --> Failed: transaction did not commit
    Completed --> Reversed: eligible compensating transaction committed
    Completed --> Completed: equivalent idempotent retry returns stored result
    Reversed --> Reversed: equivalent reversal retry returns stored result
    Rejected --> [*]
    Failed --> [*]
    Reversed --> [*]
```

No externally observable `Completed` state is stored before the ledger transaction commits.

`Rejected` and `Failed` describe request outcomes in this view; they do not require a partially persisted transfer row. The implementation must document which non-financial diagnostic/audit evidence is retained without confusing it with a committed financial operation.

## 10. Double-entry examples

The signs are presented from the platform ledger's accounting perspective.

```mermaid
flowchart LR
    subgraph Funding[Sandbox funding journal entry]
        F1[Debit<br/>Sandbox clearing asset]
        F2[Credit<br/>Customer wallet liability]
        F1 <-->|equal minor units<br/>same currency| F2
    end

    subgraph Transfer[Internal transfer journal entry]
        T1[Debit<br/>Sender wallet liability]
        T2[Credit<br/>Receiver wallet liability]
        T1 <-->|equal minor units<br/>same currency| T2
    end
```

Every concrete accounting rule must be covered by tests and reviewed for the chosen chart of accounts.

## 11. Conceptual data relationships

Dashed business references across module ownership are represented conceptually; they do not automatically require cross-schema foreign keys.

```mermaid
erDiagram
    CUSTOMER ||--o{ KYC_SUBMISSION : submits
    CUSTOMER ||--o{ WALLET : owns
    WALLET ||--|| LEDGER_ACCOUNT : maps_to
    WALLET ||--o{ TRANSFER : originates
    WALLET ||--o{ TRANSFER : receives
    TRANSFER ||--|| JOURNAL_ENTRY : accounted_by
    JOURNAL_ENTRY ||--|{ POSTING : contains
    LEDGER_ACCOUNT ||--o{ POSTING : receives
    TRANSFER ||--o| TRANSFER : reversed_by
    TRANSFER ||--|| IDEMPOTENCY_RECORD : stabilized_by
    TRANSFER ||--o{ OUTBOX_MESSAGE : emits
```

## 12. Module data ownership

```mermaid
flowchart TB
    subgraph SQL[One PostgreSQL 18 database]
        IS[identity schema]
        CS[customers schema]
        AS[accounts schema]
        LS[ledger schema]
        PS[payments schema]
        RS[risk schema]
        NS[notifications schema]
        RPS[reporting schema<br/>rebuildable projections]
        XS[integration schema<br/>outbox / inbox]
    end

    IAM[Identity and Access] --> IS
    Customers --> CS
    Accounts --> AS
    Ledger --> LS
    Payments --> PS
    Risk --> RS
    Notifications --> NS
    Reporting --> RPS
    Platform[Integration infrastructure] --> XS
```

Each module writes its own schema. Cross-module behavior passes through contracts even though the schemas share one database.

Use explicit `snake_case` mappings and module-specific migration-history tables. Roles/grants and trusted `search_path` configuration complement these ownership boundaries; schemas alone do not isolate the modules. Concrete type conventions are in [Data Architecture](data-architecture.md#51-postgresql-type-mapping-baseline).

## 13. Outbox processing lifecycle

```mermaid
stateDiagram-v2
    [*] --> Pending: stored with business transaction
    Pending --> Claimed: worker lease/claim
    Claimed --> Processed: side effect completed and recorded
    Claimed --> Pending: transient failure and retry scheduled
    Claimed --> DeadLettered: retry policy exhausted
    Claimed --> Pending: lease expires after worker crash
    Processed --> [*]
    DeadLettered --> Pending: authorized replay after investigation
```

Replay is controlled and auditable. A duplicate delivery must be harmless.

## 14. Trust boundaries and principal flows

```mermaid
flowchart LR
    subgraph Untrusted[Untrusted / external]
        Browser[Browser or API client]
        Attacker[Malicious client]
    end

    subgraph Edge[Public edge]
        Ingress[HTTPS ingress<br/>request and rate limits]
    end

    subgraph App[Application trust zone]
        API[API identity, authorization,<br/>validation, business modules]
        Worker[Worker identity<br/>outbox processing]
    end

    subgraph Data[Data trust zone]
        DB[(PostgreSQL 18<br/>least-privileged roles)]
        Secrets[Secret store / managed identity]
    end

    subgraph Ops[Operational systems]
        Telemetry[Sanitized telemetry]
        Notify[Fake notification adapter]
    end

    Browser --> Ingress --> API
    Attacker --> Ingress
    API --> DB
    Worker --> DB
    API --> Secrets
    Worker --> Secrets
    API -. allow-listed data .-> Telemetry
    Worker -. allow-listed data .-> Telemetry
    Worker --> Notify
```

Every boundary crossing validates identity, authorization, shape, sensitivity, and failure behavior appropriate to that boundary.

## 15. Public sandbox deployment

```mermaid
flowchart TB
    User[Customer / reviewer]
    DNS[Public DNS and HTTPS ingress]

    subgraph Cloud[Chosen cloud environment]
        API[Managed app/container<br/>ASP.NET Core API]
        Worker[Managed worker/container]
        SQL[(Managed PostgreSQL database)]
        Vault[Secret store / managed identity]
        Obs[Logs, metrics, traces]
    end

    GitHub[Public GitHub repository]
    Actions[GitHub Actions<br/>build, test, scan, publish, deploy]

    User --> DNS --> API
    API --> SQL
    Worker --> SQL
    API --> Vault
    Worker --> Vault
    API -. telemetry .-> Obs
    Worker -. telemetry .-> Obs
    GitHub --> Actions --> API
    Actions --> Worker
```

The final cloud service and credential model are selected in the deployment phase. This diagram does not imply a current deployment.

## 16. CI/CD and release flow

```mermaid
flowchart LR
    Change[Focused branch / pull request]
    Review[Human review<br/>requirements, diff, ADR]
    Verify[Restore, format, build,<br/>unit and architecture tests]
    Integrate[PostgreSQL integration, functional,<br/>concurrency and security tests]
    Scan[Dependency, secret,<br/>and code analysis]
    Artifact[Versioned immutable artifact]
    Stage[Staging deploy and migrations]
    Smoke[Smoke, reconciliation,<br/>observability checks]
    Approve{Release approval}
    Public[Public sandbox]
    Recover[Rollback or forward fix]

    Change --> Review --> Verify --> Integrate --> Scan --> Artifact --> Stage --> Smoke --> Approve
    Approve -->|approved| Public
    Approve -->|failed| Recover
    Public -->|release issue| Recover
```

## 17. Evolution path

```mermaid
flowchart LR
    M1[Modular monolith<br/>API + PostgreSQL]
    M2[Add worker and outbox]
    M3[Measure change, load,<br/>failure and ownership needs]
    Decision{Independent deployment<br/>has proven value?}
    Keep[Keep module in monolith]
    Extract[Extract low-risk Notifications<br/>behind versioned events]

    M1 --> M2 --> M3 --> Decision
    Decision -->|no| Keep
    Decision -->|yes| Extract
```

Ledger remains in-process unless extraordinary evidence justifies accepting distributed financial-consistency and operational complexity.

## 18. Diagram maintenance checklist

- Update context/runtime diagrams when a real external system or process is introduced.
- Update module map when an allowed dependency changes.
- Update transaction and sequence views when atomic state changes change.
- Update data views with module ownership and migration decisions.
- Update threat boundaries when hosting, identity, broker, or frontend design changes.
- Mark future/deferred elements clearly.
- Ensure diagram language matches the [Glossary](../glossary.md).
- Link the ADR that authorizes a significant change.
