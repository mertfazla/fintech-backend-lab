# Fintech Backend Lab

Fintech Backend Lab is an ASP.NET Core 10 backend for a simulated digital-wallet and internal-transfer platform. Its design centers on explicit financial invariants, transaction safety, auditable state changes, and clear module boundaries.

The system is being developed incrementally through tested vertical slices. Implemented and planned capabilities are identified separately throughout this repository.

> [!IMPORTANT]
> This repository is a simulated platform for engineering and demonstration purposes. It does not process real money, connect to banks, perform real KYC/AML checks, or store real financial or personal data.

## Current status

The runnable foundation is complete. The repository currently contains:

- an ASP.NET Core controller-based API targeting .NET 10;
- built-in OpenAPI generation and a Development-only Scalar API reference;
- a versioned system-status endpoint;
- the first module boundary for Customers;
- an HTTP functional test that starts the application in a test host;
- product, architecture, data, and security documentation.

PostgreSQL persistence and the business features are not implemented yet. Authentication, customer onboarding, wallets, transfers, the ledger, CI, and deployment remain on the roadmap.

## Engineering focus

Financial workflows introduce failure modes that ordinary request-response examples rarely expose. The design therefore focuses on:

- precise money representation without floating-point arithmetic;
- atomic and idempotent execution of money-moving commands;
- concurrency control that prevents overspending and lost updates;
- resource-level authorization across customer-owned data;
- append-only accounting history and compensating reversals.

Architecture decisions may be documented before implementation, but repository status is based on runnable code and automated tests.

## Architecture

The target is a modular monolith. Each business module owns its rules and data, while the application remains one deployable system. The solution is intentionally small at the moment:

```text
FunctionalTests
      |
      v
     API  ------>  Customers
```

The dependency direction is deliberate:

- `FintechBackend.Api` hosts HTTP endpoints and composes the application.
- `FintechBackend.Customers` will own customer profiles and simulated onboarding.
- `FintechBackend.FunctionalTests` verifies behavior through the HTTP boundary.

Future wallet, payment, and ledger behavior will be added as separate business modules only when the corresponding feature is implemented.

## Technology

### In use

| Area | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core 10 |
| API style | Controllers |
| API contract | Built-in ASP.NET Core OpenAPI |
| Interactive API reference | Scalar |
| Testing | xUnit and `Microsoft.AspNetCore.Mvc.Testing` |
| Source control | Git |

### Planned

| Area | Technology |
|---|---|
| Database | PostgreSQL 18 |
| Data access | EF Core 10 with Npgsql |
| Database integration tests | Testcontainers for PostgreSQL |
| Observability | OpenTelemetry |
| CI | GitHub Actions |

Planned tools are introduced only when a feature creates a real need for them.

## Repository structure

```text
FinTech.slnx
src/
  FintechBackend.Api/
  Modules/
    Customers/
      FintechBackend.Customers/
tests/
  FintechBackend.FunctionalTests/
docs/
  adr/
  architecture/
  tutorials/
```

## Run locally

### Requirements

- .NET 10 SDK
- Visual Studio with ASP.NET and web development support, or another .NET-compatible editor

PostgreSQL is not required for the current foundation.

### Restore and run

From the repository root:

```powershell
dotnet restore FinTech.slnx
dotnet run --project src/FintechBackend.Api/FintechBackend.Api.csproj --launch-profile https
```

The local development URLs are:

- API: `https://localhost:7060`
- Scalar: `https://localhost:7060/scalar/v1`
- OpenAPI document: `https://localhost:7060/openapi/v1.json`

If the local HTTPS development certificate is not trusted, follow the .NET SDK prompt or configure the certificate before continuing.

## Available endpoint

### System status

```http
GET /api/v1/system/status
```

Example response:

```json
{
  "application": "Fintech Backend Lab",
  "status": "Running"
}
```

This endpoint proves the HTTP pipeline and test setup. It is not a complete production health-check implementation.

## Run the tests

```powershell
dotnet test FinTech.slnx
```

The current functional test starts the API in memory, sends an HTTP request to the status endpoint, and verifies the response status, media type, and JSON body.

## Documentation

Detailed decisions and requirements live under [`docs`](docs/README.md):

- [Product brief](docs/product-brief.md)
- [Delivery roadmap](docs/roadmap.md)
- [Software architecture](docs/architecture/software-architecture.md)
- [Data architecture](docs/architecture/data-architecture.md)
- [Security architecture](docs/architecture/security-architecture.md)
- [Architecture diagrams](docs/architecture/diagrams.md)
- [Architecture Decision Records](docs/adr/README.md)
- [Project foundation tutorial](docs/tutorials/01-project-foundation.md)

## Roadmap

The next milestones are:

1. finish the HTTP foundation with consistent errors and operational endpoints;
2. connect PostgreSQL through EF Core and Npgsql;
3. implement fictional customer registration and simulated onboarding;
4. model money and wallets with explicit invariants;
5. introduce a balanced, immutable double-entry ledger;
6. build an idempotent internal-transfer workflow;
7. add concurrency, security, integration, and failure tests;
8. add observability, CI, containers, and a public demo deployment.

No frontend is planned until the backend workflow is reliable and demonstrable through its API.

## Project boundaries

- All customers, funds, identity data, and providers are fictional.
- The project is not production-ready or compliant with financial regulations.
- Real card data, bank credentials, identity documents, and personal information must never be added.
- Architecture documents describe intent; tests and runnable code provide implementation evidence.

## License

Licensed under the [MIT License](LICENSE).
