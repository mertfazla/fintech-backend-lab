# ADR 0005 — Use a Backend-First ASP.NET Core Controller API

- **Status:** Accepted
- **Date:** 2026-08-30
- **Decision owners:** Repository owner

## Context

The project is intended primarily to teach backend engineering and technical-interview skills. A frontend would add a second ecosystem and could hide API/security problems behind UI behavior. The backend still needs a clear, discoverable client contract.

ASP.NET Core supports both controllers and Minimal APIs. Both are valid. Controllers expose familiar model binding, filters, action conventions, authorization attributes/policies, API-controller behavior, and testing concepts commonly encountered in .NET interviews and enterprise codebases.

## Decision drivers

- backend learning focus;
- explicit HTTP/API boundary;
- strong interview coverage;
- generated OpenAPI as contract source of truth;
- support for a later independent frontend;
- avoidance of duplicate API-documentation tools.

## Decision

- Build an ASP.NET Core 10 Web API using controllers for Version 1.
- Organize controllers/endpoints by module and vertical use case rather than one global CRUD controller layer.
- Use built-in ASP.NET Core OpenAPI as the contract source of truth.
- Use Scalar only as a Development-only renderer/explorer under the working agreement.
- Do not build a frontend until core financial/security/test/deployment gates are met.
- Use generated OpenAPI and automated functional tests as the primary contract verification.
- Add a thin frontend later without moving any financial rule to the client.

## Alternatives considered

### Minimal APIs

**Advantages:** concise endpoint definitions, strong modern ASP.NET Core support, good fit for focused services.

**Not selected for primary Version 1:** controllers give broader explicit interview practice for the learning goals. A focused comparison spike/ADR may revisit this without rewriting the domain/application layers.

### Frontend and backend together from day one

**Advantages:** visible demo earlier.

**Not selected:** divides learning attention, expands scope, and can encourage validation/security assumptions in the client.

### Backend-for-frontend/server-rendered UI only

**Advantages:** one deployable UI/application.

**Not selected:** weakens the goal of designing a reusable, explicit backend API contract.

### Multiple API explorer tools

**Rejected:** overlapping sources and configuration create maintenance noise. One OpenAPI source and one optional renderer are sufficient.

## Consequences

### Positive

- Backend can be developed and tested independently.
- HTTP behavior and authorization remain visible.
- OpenAPI supports review and a future client.
- Controllers provide a familiar learning surface.

### Negative

- Public demo is less visual until the optional frontend phase.
- Controller conventions can encourage fat controllers if boundaries are not enforced.
- Generated OpenAPI must be tested and reviewed.
- Controller/Minimal API differences still need separate interview study.

## Guardrails

- Controllers contain transport concerns, not financial business logic.
- Dedicated request/response contracts; no EF/domain entity exposure.
- Problem Details and stable error semantics.
- Every protected endpoint has policy/resource authorization tests.
- Runtime OpenAPI/Scalar remains Development-only by default.
- No credentials in API-reference configuration or examples.
- Frontend is never trusted to enforce invariants.

## Validation

- OpenAPI generation succeeds in CI and exposes expected operations/contracts.
- Functional tests verify the real HTTP pipeline.
- A reviewer can execute core flows without a frontend.
- Controllers remain thin and feature/module ownership is visible.

## Review triggers

- measured controller friction for module/vertical-slice organization;
- need for Native AOT or a deployment profile that changes the trade-off;
- a new separately deployed focused API;
- frontend contract generation requirements;
- evidence that Minimal APIs improve the project without obscuring learning goals.

## Related documents

- [Software architecture](../architecture/software-architecture.md)
- [Request-path diagram](../architecture/diagrams.md#5-aspnet-core-request-path)
- [Root working agreement](../../README.md#engineering-working-agreement)

