# 01 — Create the Project Foundation Yourself

> A guided first implementation for Fintech Backend Lab: ASP.NET Core 10, controllers, a Customers module, Scalar, and HTTP functional tests.

**Status:** Instructions and reference snippets only. These application files have not been created or executed by the assistant. You type the files and run the commands yourself.

**Scope:** The complete first runnable foundation, not the complete fintech application. PostgreSQL remains the selected database, but this milestone deliberately has no persistence, authentication, customers, wallets, or money movement yet.

**Baseline checked:** 2026-08-31. The local machine has SDK `10.0.400` and ASP.NET Core runtime `10.0.11`. Package versions below were checked against NuGet. Availability is not a substitute for restore, build, test, or a vulnerability review.

## 1. What to build first

Start with a running, understandable application skeleton. Your [product brief](../product-brief.md), [architecture](../architecture/software-architecture.md), and [ADRs](../adr/README.md) already provide the initial design. Review them; do not restart the architecture exercise before writing your first endpoint.

By the end of this guide, you should be able to:

- open one solution in Visual Studio;
- explain the purpose of its three projects;
- start a controller-based API over local HTTPS;
- call a harmless status endpoint through Scalar;
- receive a Problem Details response for an unknown route;
- run repeatable HTTP tests without PostgreSQL;
- show that Scalar and OpenAPI routes are absent in the Production environment;
- commit a clean foundation without secrets or generated build output.

Work in three passes:

1. **Solution and build setup:** Sections 2–7.
2. **Running API:** Sections 8–11.
3. **Verification and handoff:** Sections 12–17.

Read each snippet, type it, and explain it aloud. The terminal scaffolding commands generate standard templates; the application behavior is yours to write. If you want a stricter exercise, read a snippet, close it, write your version, and then compare.

Do not install Redis, RabbitMQ, MediatR, AutoMapper, a frontend, or an identity server during this milestone. None is needed to demonstrate the behavior above.

## 2. Understand the pieces before creating them

| Term | Meaning in this project |
|---|---|
| Repository | The versioned directory containing code, tests, documentation, and configuration. |
| Solution | `FintechBackendLab.slnx`: groups projects for Visual Studio and CLI operations. It is not the running application. |
| Project | A `.csproj` file defining one buildable unit and its dependencies. |
| Assembly | The compiled output, usually a `.dll`. A class library does not start an HTTP server by itself. |
| Namespace | A logical C# name such as `FintechBackend.Customers`. A folder or namespace alone is not an access-control boundary. |
| Project reference | A dependency on another project in this solution. |
| Package reference | A dependency supplied through NuGet. |
| SDK | Tools for creating, restoring, building, testing, and publishing projects. |
| Runtime | Components needed to execute an application. |
| Composition root | Startup code that assembles the application's services and modules. Here, it starts in `Program.cs`. |

Create only these three projects initially:

| Project | Responsibility | References |
|---|---|---|
| `FintechBackend.Api` | Host HTTP, configure middleware, expose diagnostics, compose modules. | Customers module |
| `FintechBackend.Customers` | Eventually own customer lifecycle and simulated KYC. Initially only an assembly marker. | No application projects |
| `FintechBackend.FunctionalTests` | Exercise the API through its HTTP pipeline. | API |

The dependency direction is `FunctionalTests → Api → Customers`. Customers must not reference API or tests.

The first tests are **functional tests**, not unit tests: they send HTTP requests through an in-process application host. Unit tests will be added when there are domain rules worth testing. Database integration tests come with persistence.

### Initial file structure

The tree below describes the result **after you complete the guide**. It is not the current implementation state.

```text
FinTech/
├── README.md
├── docs/
├── .gitignore
├── .editorconfig
├── global.json
├── Directory.Build.props
├── Directory.Packages.props
├── FintechBackendLab.slnx
├── src/
│   ├── FintechBackend.Api/
│   │   ├── FintechBackend.Api.csproj
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   ├── appsettings.Development.json
│   │   ├── Properties/
│   │   │   └── launchSettings.json
│   │   └── Features/
│   │       └── SystemStatus/
│   │           ├── SystemStatusController.cs
│   │           └── SystemStatusResponse.cs
│   └── Modules/
│       └── Customers/
│           └── FintechBackend.Customers/
│               ├── FintechBackend.Customers.csproj
│               └── CustomersModule.cs
└── tests/
    └── FintechBackend.FunctionalTests/
        ├── FintechBackend.FunctionalTests.csproj
        └── FoundationTests.cs
```

Restore will also generate package lock files for projects with package dependencies. Commit those lock files; do not commit `bin/` or `obj/`.

### How the Customers module grows later

```text
FintechBackend.Customers/
├── Contracts/                  Public, stable module contracts
├── Domain/                     Customer rules and lifecycle
├── Features/
│   ├── RegisterCustomer/        One use case
│   └── SubmitKyc/               Another use case
├── Infrastructure/
│   └── Persistence/            EF mappings, context, migrations
├── Configuration/              Module registration when needed
└── CustomersModule.cs
```

Do not create those empty folders now. Add them when their first real file exists. Git does not track empty directories.

This is a modular monolith with vertical slices. Clean Architecture describes dependency direction; it does not require separate `Domain`, `Application`, and `Infrastructure` projects for every module. With one assembly per module, namespace-level architecture tests and review will also be needed to prevent domain code from depending on infrastructure.

The API's system-status feature is host infrastructure. Future customer use cases belong to Customers; wallet use cases belong to Accounts. Do not let `Api/Features` become the home of every business feature.

## 3. Check the tools and choose one creation workflow

Use PowerShell for the commands in this guide. A normal terminal or Visual Studio's terminal is fine. Keep every command at the repository root unless a step explicitly says otherwise.

```powershell
Set-Location 'C:\Users\ASUS\Documents\Projects-Side\FinTech'
dotnet --version
dotnet --list-sdks
git --version
```

The SDK was checked as `10.0.400` on this machine. There are no `.csproj` or solution files in the current checkout. The existing `.vs` directory is Visual Studio metadata, not an application project; leave it alone.

In Visual Studio Installer, check that the **ASP.NET and web development** workload is available for your Visual Studio 2026 installation. The CLI works with the installed SDK; the workload provides the corresponding IDE experience. [Microsoft workload reference](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community?view=visualstudio)

We will use the CLI to create files, then open the solution in Visual Studio. Do not also create another solution through the New Project wizard. That can accidentally produce a second nested `FinTech/FinTech` directory.

### Pin the SDK

Create `global.json` in the repository root:

```json
{
  "sdk": {
    "version": "10.0.400",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

This selects the installed SDK feature band and permits later servicing patches within that band. It does not automatically select .NET 11 or another .NET 10 feature band. It pins the SDK policy, not NuGet packages or the runtime on a deployment server. Another machine must have an SDK satisfying this policy. [Microsoft: global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)

Run `dotnet --version` again from the repository root and confirm it resolves successfully.

## 4. Create the solution and projects

Run these commands yourself, one at a time. Do not use `--force`; if a file already exists, inspect it before proceeding.

```powershell
dotnet new sln --name FintechBackendLab --format slnx

dotnet new webapi --name FintechBackend.Api --output src/FintechBackend.Api --framework net10.0 --use-controllers --no-restore

dotnet new classlib --name FintechBackend.Customers --output src/Modules/Customers/FintechBackend.Customers --framework net10.0 --no-restore

dotnet new xunit --name FintechBackend.FunctionalTests --output tests/FintechBackend.FunctionalTests --framework net10.0 --no-restore
```

Important options:

- `--use-controllers` selects our agreed HTTP programming model.
- `--framework net10.0` selects the project's target framework.
- `--output` chooses the actual directory, not just a solution-folder label.
- `--no-restore` lets us finish package configuration before the first restore.
- `--format slnx` makes the solution format explicit; .NET 10 uses it by default. [Microsoft: dotnet sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln)

Add the projects to the solution:

```powershell
dotnet sln FintechBackendLab.slnx add src/FintechBackend.Api/FintechBackend.Api.csproj
dotnet sln FintechBackendLab.slnx add src/Modules/Customers/FintechBackend.Customers/FintechBackend.Customers.csproj
dotnet sln FintechBackendLab.slnx add tests/FintechBackend.FunctionalTests/FintechBackend.FunctionalTests.csproj
```

Add references in the intended direction:

```powershell
dotnet add src/FintechBackend.Api/FintechBackend.Api.csproj reference src/Modules/Customers/FintechBackend.Customers/FintechBackend.Customers.csproj
dotnet add tests/FintechBackend.FunctionalTests/FintechBackend.FunctionalTests.csproj reference src/FintechBackend.Api/FintechBackend.Api.csproj
dotnet sln FintechBackendLab.slnx list
```

The final command should list exactly three projects. A project can exist on disk without being in a solution. Adding it to a solution does not automatically create a project reference.

Open `FintechBackendLab.slnx` using **File → Open → Project/Solution** in Visual Studio. Set `FintechBackend.Api` as the startup project. Visual Studio may attempt background package restore while you edit; the first authoritative restore is in Section 10 after configuration is complete.

## 5. Establish repository-wide build and formatting settings

Create `Directory.Build.props` in the repository root:

```xml
<Project>
  <PropertyGroup>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <EnableNETAnalyzers>true</EnableNETAnalyzers>
    <AnalysisLevel>10.0</AnalysisLevel>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <Deterministic>true</Deterministic>
    <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  </PropertyGroup>
</Project>
```

These settings apply to projects beneath this directory. Nullable annotations help detect possible null mistakes; they do not make invalid inputs impossible. Warnings-as-errors means you should investigate warnings rather than suppressing them broadly. Lock files record resolved package dependencies.

Keep `TargetFramework` in each `.csproj` so it is visible when learning the individual projects. `net10.0` selects the normal C# 14 language baseline; there is no need to set `LangVersion` to `latest` or `preview`.

Create `.editorconfig` in the repository root:

```ini
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true
indent_style = space
indent_size = 4

[*.{json,yml,yaml,csproj,props,slnx}]
indent_size = 2

[*.md]
trim_trailing_whitespace = false

[*.cs]
csharp_style_namespace_declarations = file_scoped:suggestion
dotnet_sort_system_directives_first = true
```

Formatting is a shared convention, not architecture. Start with a small understandable configuration; avoid importing hundreds of unexplained rules.

Create the standard .NET/Visual Studio ignore file:

```powershell
dotnet new gitignore
```

Inspect it. It should exclude `.vs`, `bin`, and `obj`. Add these rules yourself if equivalent rules are not already present:

```gitignore
# Local secrets and environment overrides
.env
.env.*
!.env.example

# Private certificates, keys, and database exports
*.pfx
*.p12
*.pem
*.key
*.dump
*.backup

# Local outputs
/artifacts/
/TestResults/
```

A `.gitignore` is not a secret scanner, and it does not remove files already tracked by Git. Never put secrets in a tracked file just because you expect to ignore it later. Do not ignore `packages.lock.json`.

## 6. Define the package baseline

Create `Directory.Packages.props` in the repository root:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.11" />
    <PackageVersion Include="Scalar.AspNetCore" Version="2.17.2" />
    <PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.11" />
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.9.0" />
    <PackageVersion Include="xunit" Version="2.9.3" />
    <PackageVersion Include="xunit.runner.visualstudio" Version="4.0.0" />
  </ItemGroup>
</Project>
```

This is the checked baseline for this walkthrough, not a promise that these will always be the newest or vulnerability-free versions. Review dependency updates deliberately, then regenerate and review lock files. Do not use floating versions in committed package configuration.

| Package | Why it exists here |
|---|---|
| [Microsoft.AspNetCore.OpenApi](https://www.nuget.org/packages/Microsoft.AspNetCore.OpenApi/10.0.11) | Generates the API contract. |
| [Scalar.AspNetCore](https://www.nuget.org/packages/Scalar.AspNetCore/2.17.2) | Displays and explores that contract during Development. |
| [Microsoft.AspNetCore.Mvc.Testing](https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing/10.0.11) | Hosts the API for HTTP tests through `WebApplicationFactory`. |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.9.0) | Connects this test project to the test platform. |
| [xunit](https://www.nuget.org/packages/xunit/2.9.3) | Provides test declarations and assertions. |
| [xunit.runner.visualstudio](https://www.nuget.org/packages/xunit.runner.visualstudio/4.0.0) | Discovers and runs xUnit tests in Visual Studio/VSTest. |

The installed SDK's `xunit` template uses **xUnit v2**. This guide keeps that framework and the default VSTest path. The checked Visual Studio runner supports xUnit v2 even though its own version is `4.0.0`; package version numbers are not framework version requirements. An xUnit v3/Microsoft.Testing.Platform migration can be a separate decision, not an accidental mix of templates.

Central package management specifies versions once, while individual projects specify which packages they need. `PackageReference` entries below therefore have **no `Version` attribute**. Do not leave template versions behind alongside the central versions. [Microsoft: Central Package Management](https://learn.microsoft.com/en-us/nuget/consume-packages/central-package-management)

Do not add Npgsql or EF Core yet. When persistence starts, use EF Core 10 and `Npgsql.EntityFrameworkCore.PostgreSQL` 10.x as required by [ADR 0006](../adr/0006-use-postgresql-and-npgsql.md).

## 7. Review and replace the generated project files

Use these complete contents for the newly generated `.csproj` files. The project references repeat the references you added with the CLI; keep each one only once.

### API project

File: `src/FintechBackend.Api/FintechBackend.Api.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" />
    <PackageReference Include="Scalar.AspNetCore" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="../Modules/Customers/FintechBackend.Customers/FintechBackend.Customers.csproj" />
  </ItemGroup>
</Project>
```

### Customers project

File: `src/Modules/Customers/FintechBackend.Customers/FintechBackend.Customers.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
</Project>
```

This library does not need EF Core or ASP.NET Core for its current marker type. When a real module feature needs HTTP transport or persistence, add only the references that implementation requires. Domain types must remain independent of those technologies even when they share a module assembly.

### Functional test project

File: `tests/FintechBackend.FunctionalTests/FintechBackend.FunctionalTests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <Using Include="Xunit" />
    <ProjectReference Include="../../src/FintechBackend.Api/FintechBackend.Api.csproj" />
  </ItemGroup>
</Project>
```

The template's `coverlet.collector` is intentionally omitted from this first test baseline. Add coverage tooling when you begin assessing meaningful test coverage. `PrivateAssets` prevents the runner from flowing to consumers; tests are also marked non-packable.

### Remove only the sample files you just generated

After reading them, remove these template files through Visual Studio if they exist:

- `src/FintechBackend.Api/WeatherForecast.cs`
- `src/FintechBackend.Api/Controllers/WeatherForecastController.cs`
- the generated weather-request `.http` file inside `src/FintechBackend.Api/`
- `src/Modules/Customers/FintechBackend.Customers/Class1.cs`
- `tests/FintechBackend.FunctionalTests/UnitTest1.cs`

An empty `Controllers` folder can be removed. Do not delete unrelated files or existing documentation. The sample weather endpoint and empty passing test are not project features.

## 8. Write the initial module and API

### 8.1 Give the module an assembly marker

File: `src/Modules/Customers/FintechBackend.Customers/CustomersModule.cs`

```csharp
namespace FintechBackend.Customers;

// Identifies this module's assembly to the application's composition root.
public static class CustomersModule
{
}
```

This marker lets the API refer to the module's assembly without depending on a business entity. It has no customer behavior and registers no fake services. The composition root uses it for controller discovery; there are no customer controllers to discover yet. Later, a module registration method can register real services when they exist.

### 8.2 Define the diagnostic response

File: `src/FintechBackend.Api/Features/SystemStatus/SystemStatusResponse.cs`

```csharp
namespace FintechBackend.Api.Features.SystemStatus;

public sealed record SystemStatusResponse(string Application, string Status);
```

This is an HTTP response contract, not a database entity. A record is appropriate for this small immutable data shape. It says nothing about financial health or database connectivity.

### 8.3 Create a controller

File: `src/FintechBackend.Api/Features/SystemStatus/SystemStatusController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;

namespace FintechBackend.Api.Features.SystemStatus;

[ApiController]
[Route("api/v1/system")]
[Produces("application/json")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SystemStatusController : ControllerBase
{
    [HttpGet("status")]
    [ProducesResponseType(typeof(SystemStatusResponse), StatusCodes.Status200OK)]
    public ActionResult<SystemStatusResponse> GetStatus()
    {
        return Ok(new SystemStatusResponse("Fintech Backend Lab", "Running"));
    }
}
```

Read it as: an HTTP `GET` to `/api/v1/system/status` returns a `200 OK` JSON response. The folder does not determine the route; the route attributes do.

The action is synchronous because it performs no asynchronous I/O. Do not add `async`, `Task.Run`, a repository, or a service interface just to make this example look more complicated. Future database actions will need asynchronous I/O and cancellation propagation.

### 8.4 Compose the application

Replace the newly generated `src/FintechBackend.Api/Program.cs` with:

```csharp
using System.Diagnostics;
using FintechBackend.Customers;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddApplicationPart(typeof(CustomersModule).Assembly);

builder.Services.AddOpenApi();

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions.TryAdd(
            "traceId",
            Activity.Current?.Id ?? context.HttpContext.TraceIdentifier);

        if (context.ProblemDetails.Status is >= 500)
        {
            context.ProblemDetails.Title = "An unexpected error occurred.";
            context.ProblemDetails.Detail = null;
            context.ProblemDetails.Extensions.TryAdd("code", "server.unexpected");
        }
    };
});

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Fintech Backend Lab API");
        options.DisableAgent();
        options.DisableDefaultFonts();
    });
}

app.MapControllers();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.Run();

// Gives WebApplicationFactory a public entry-point type for HTTP tests.
public partial class Program
{
}
```

Read the file in this order:

1. `CreateBuilder` prepares hosting, configuration, logging, and the service collection.
2. `builder.Services` registrations describe services the application can resolve.
3. `Build` creates the application and service provider.
4. `Use...` configures request middleware.
5. `Map...` defines endpoints.
6. `Run` starts the host.

`AddControllers` alone does not expose controller routes; `MapControllers` is also required. `AddApplicationPart` points controller discovery at the Customers assembly. It neither implements that module nor creates cross-module service registrations.

`UseExceptionHandler` handles unhandled downstream request exceptions. `UseStatusCodePages` can supply bodies for otherwise empty error responses, such as an unknown route. `AddProblemDetails` supplies the shared error representation; the customization adds a trace identifier and a generic server-error title. Response formatting depends on content negotiation. These mechanisms do not automatically define your future business-error codes or handle startup failures. [Microsoft: error handling](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)

Framework exception logging is still server-side logging: before adding sensitive data, review log contents and access controls. Never add exception messages, request bodies, connection strings, or tokens to client-facing Problem Details.

OpenAPI generates the contract; Scalar reads it. Both routes are mapped only in Development. Scalar's agent and default external font loading are disabled, and no credentials are prefilled. This is not an authentication system or a claim that browser activity has been audited. [Microsoft: OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0), [Scalar: ASP.NET Core integration](https://scalar.com/products/api-references/integrations/aspnetcore/integration)

The health predicate excludes all dependency checks. `/health/live` answers only whether this HTTP process can respond. There is deliberately **no `/health/ready` yet**: add it with an actual PostgreSQL check in the persistence milestone. A successful liveness response must never be presented as proof of database readiness. [Microsoft: health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)

Do not add empty authentication configuration. This starter has no customer or financial endpoints. Before adding them, implement the agreed authentication and deny-by-default authorization policies. Before deployment, review proxy/HTTPS configuration, operational endpoint exposure, rate limits, and the rest of the security baseline; HSTS alone does not make this starter deployment-ready.

## 9. Configure local execution

### Application defaults

File: `src/FintechBackend.Api/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "localhost;127.0.0.1;[::1]"
}
```

File: `src/FintechBackend.Api/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

These files contain non-secret defaults only. `AllowedHosts` filters the HTTP Host header; it is not a firewall or authentication. Configure actual deployment hostnames later. Do not change it to a wildcard just to avoid diagnosing an unexpected request host.

### Local launch profile

File: `src/FintechBackend.Api/Properties/launchSettings.json`

```json
{
  "profiles": {
    "https": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "scalar",
      "applicationUrl": "https://localhost:7180;http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

This profile is for local development. Visual Studio uses its browser-launch settings; when using the CLI, open the URL yourself if no browser opens. `launchSettings.json` is not deployment configuration, and a Release build does not automatically mean the host environment is Production.

The profile binds loopback addresses. Do not switch to `0.0.0.0`, publish a tunnel, or expose Development mode publicly.

Check and, if needed, trust the development HTTPS certificate:

```powershell
dotnet dev-certs https --check --trust
```

If the check reports no valid trusted certificate, run:

```powershell
dotnet dev-certs https --trust
```

Trusting a certificate changes the local certificate trust configuration and may display a Windows confirmation. This certificate is only for local development. Do not export or commit its private key, and do not disable certificate validation as a general fix. [Microsoft: dotnet dev-certs](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs)

## 10. Restore, build, and start the API

From the repository root:

```powershell
dotnet restore FintechBackendLab.slnx
dotnet build FintechBackendLab.slnx --no-restore
dotnet run --project src/FintechBackend.Api/FintechBackend.Api.csproj --launch-profile https
```

Read the console output. It should identify the listening HTTPS and HTTP addresses. Keep the terminal running while using the API; press `Ctrl+C` to stop it.

The first restore downloads packages and writes lock files. Commit them after reviewing changes. Subsequent reproducibility checks can use:

```powershell
dotnet restore FintechBackendLab.slnx --locked-mode
```

Locked mode should fail when dependency resolution would change. When deliberately updating packages, run a normal restore, inspect the lock-file changes, and test before committing. Never delete lock files merely to hide an unexplained restore failure.

If you prefer Visual Studio, select the `https` launch profile for `FintechBackend.Api` and use **Start Without Debugging**. Later, use the debugger and place a breakpoint inside `GetStatus` to follow a request.

## 11. Explore the behavior through Scalar

Open these local addresses after the application starts:

| Address | Expected behavior |
|---|---|
| `https://localhost:7180/scalar` | Development-only interactive API reference. |
| `https://localhost:7180/openapi/v1.json` | Development-only OpenAPI document. |
| `https://localhost:7180/api/v1/system/status` | `200 OK` with the diagnostic response. |
| `https://localhost:7180/health/live` | `200 OK`, normally with the body `Healthy`. |
| `https://localhost:7180/health/ready` | `404`: database readiness has not been implemented. |

In Scalar, find `GET /api/v1/system/status` and send a request. Expected response:

```json
{
  "application": "Fintech Backend Lab",
  "status": "Running"
}
```

For an intentional error check, request an unknown path with `Accept: application/json`. The automated test below does exactly that and checks for `404` plus `application/problem+json`. A browser navigation can request HTML instead, so do not assume every client negotiates the same error format.

Scalar helps you explore the API. It does not prove that regressions will be caught tomorrow. That is the next section's job. Health-check endpoints are operational routes and are not necessarily listed in the generated OpenAPI document.

## 12. Write meaningful functional tests

File: `tests/FintechBackend.FunctionalTests/FoundationTests.cs`

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FintechBackend.FunctionalTests;

public sealed class FoundationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public FoundationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Production")]
    public async Task Status_returns_the_expected_contract(string environment)
    {
        using var app = CreateApplication(environment);
        using var client = CreateClient(app);

        using var response = await client.GetAsync("/api/v1/system/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Fintech Backend Lab", body.RootElement.GetProperty("application").GetString());
        Assert.Equal("Running", body.RootElement.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Liveness_returns_healthy_without_external_dependencies()
    {
        using var app = CreateApplication("Production");
        using var client = CreateClient(app);

        using var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Unknown_route_returns_problem_details_with_a_trace_id()
    {
        using var app = CreateApplication("Production");
        using var client = CreateClient(app);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/route-that-does-not-exist");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task OpenApi_in_development_describes_the_status_route()
    {
        using var app = CreateApplication("Development");
        using var client = CreateClient(app);

        using var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = body.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/v1/system/status", out _));
    }

    [Fact]
    public async Task Scalar_in_development_serves_html()
    {
        using var app = CreateApplication("Development");
        using var client = CreateClient(app);

        using var response = await client.GetAsync("/scalar/v1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("/scalar")]
    [InlineData("/scalar/v1")]
    [InlineData("/openapi/v1.json")]
    public async Task Documentation_routes_are_not_mapped_in_production(string path)
    {
        using var app = CreateApplication("Production");
        using var client = CreateClient(app);

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private WebApplicationFactory<Program> CreateApplication(string environment)
    {
        return _factory.WithWebHostBuilder(builder => builder.UseEnvironment(environment));
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> app)
    {
        return app.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });
    }
}
```

The client sends requests to an in-memory test server, not the manually running process on port 7180. The HTTPS base address avoids testing a redirect when we intend to test an endpoint; it does not verify a real TLS handshake or certificate. Disabling automatic redirects makes unexpected `3xx` responses visible.

`public partial class Program` makes the application's entry-point type accessible to `WebApplicationFactory`. The API project reference lets the test project find it. [Microsoft: ASP.NET Core integration tests](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)

The test code checks JSON property names instead of deserializing into the same server DTO, so the public response shape is part of the assertion. Each test declares its environment rather than relying on local launch settings.

`[Fact]` describes one test case; `[Theory]` runs a test for multiple inputs. This file defines nine cases after expanding the theories. If the count differs, check for leftover template tests or missing discovery. [xUnit: v2 getting started](https://xunit.net/docs/getting-started/v2/getting-started)

Run:

```powershell
dotnet test FintechBackendLab.slnx --no-restore
```

Then perform a Release verification:

```powershell
dotnet restore FintechBackendLab.slnx --locked-mode
dotnet build FintechBackendLab.slnx --configuration Release --no-restore
dotnet test FintechBackendLab.slnx --configuration Release --no-build --no-restore
dotnet format FintechBackendLab.slnx --verify-no-changes --no-restore
dotnet list FintechBackendLab.slnx package --vulnerable --include-transitive
```

If formatting reports changes, inspect them and either correct the files yourself or run `dotnet format FintechBackendLab.slnx --no-restore`, which **edits source files**. Rebuild and retest afterward. Investigate dependency audit warnings; the absence of a reported vulnerability is not a security certification.

These tests have not been run for you. A green local result is your first evidence that the snippets work in your actual setup. They do not yet test unhandled `500` responses, authorization, database behavior, browser rendering, or financial correctness.

## 13. Prepare PostgreSQL without mixing it into HTTP setup

PostgreSQL is still the active database. This chapter does not replace it with SQLite, an in-memory provider, or SQL Server. It postpones persistence until you can explain the host and HTTP pipeline.

On the inspected machine, the Docker CLI is available, but the Docker engine was not checked. `psql` was not on `PATH`; that does **not** prove PostgreSQL is uninstalled. Before the persistence milestone, confirm one PostgreSQL 18 server through pgAdmin or `psql`.

If you already installed PostgreSQL locally, check the executable at its usual Windows location:

```powershell
Test-Path 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
```

If that returns `True`, the following command checks the client version:

```powershell
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' --version
```

The client version alone does not prove the server is reachable. Connect using your own existing local administrative account; omit passwords from commands and let `psql` prompt:

```powershell
& 'C:\Program Files\PostgreSQL\18\bin\psql.exe' --host localhost --port 5432 --username YOUR_EXISTING_LOCAL_ROLE --dbname postgres --password
```

`YOUR_EXISTING_LOCAL_ROLE` is a placeholder, not a role this guide created. For a container installation or another port, use that installation's connection details. Inside the SQL prompt, verify:

```sql
SELECT version(), current_database(), current_user;
```

Exit `psql` with `\q`. These instructions use your existing database installation; they do not create schemas or change privileges. If there is no reachable server, stop before persistence and choose a local installation or a version-pinned container setup. [PostgreSQL: psql](https://www.postgresql.org/docs/18/app-psql.html)

### The next persistence checkpoint

Complete these as the next guided implementation, not as assumptions hidden inside this starter:

1. Select and record a supported PostgreSQL 18 patch/container image.
2. Create a dedicated fictional-data database, a migration/owner role, and a separate non-owner runtime role. Never use an administrative/superuser account as the API account.
3. Store local connection secrets outside tracked configuration; keep deployment secrets in a deployment secret store. Do not commit credentials or place them in Scalar configuration.
4. Add compatible EF Core 10/Npgsql 10.x packages to the Customers module's persistence infrastructure. Pin `dotnet-ef` as a local tool when migrations are introduced.
5. Create a `CustomersDbContext`, explicit `customers` schema/table mappings, and a separate `customers.__ef_migrations_history` history table.
6. Define the first real customer model and constraints before generating its migration. Review generated SQL and apply it deliberately; do not run application-startup migrations with the runtime role.
7. Add `/health/ready` with a real database connectivity check and document what it does and does not prove. Keep database checks out of `/health/live`.
8. Add real-engine PostgreSQL integration tests for mappings, constraints, migrations, and UTC behavior. Do not treat the EF in-memory provider as evidence of PostgreSQL semantics.

Follow the existing [data architecture](../architecture/data-architecture.md) for `uuid`, integer minor units, `timestamptz`, schema ownership, grants, and concurrency. The [Npgsql EF Core 10 release notes](https://www.npgsql.org/efcore/release-notes/10.0.html) describe the provider baseline.

Later financial slices need ledger balancing, ownership authorization, idempotency, atomic transactions, and concurrent-spending tests. Simply obtaining a PostgreSQL connection proves none of those properties.

## 14. Troubleshoot by checking the failing layer

| Symptom | What to inspect first |
|---|---|
| SDK requested by `global.json` cannot be found | Run `dotnet --list-sdks` from the repository root and compare with the selected feature band. |
| NuGet complains about versions under central management | Remove `Version` attributes from `PackageReference`; keep versions in `Directory.Packages.props`. |
| A package fails to download | Check package sources, proxy/network, and the exact version; do not silently select a prerelease. |
| `Program` is inaccessible to the tests | Check the API project reference and the public partial `Program` declaration. |
| No tests are discovered | Check `IsTestProject`, test SDK/runner references, `[Fact]`/`[Theory]`, and that the test project is in the solution. |
| Scalar returns `404` | Check the actual host environment, not just Debug/Release; Scalar is absent outside Development. |
| Scalar opens but cannot load the API document | Open `/openapi/v1.json` and inspect startup logs and OpenAPI errors. |
| The status route returns `404` | Check the route attributes, public controller, `AddControllers`, `MapControllers`, and that you are calling the intended process/port. |
| A functional test receives `307` | Check that the test client base address uses HTTPS and is not following redirects. |
| A request receives `400` before the controller | Check the Host header against `AllowedHosts` and any input-validation error. |
| Browser reports an untrusted HTTPS certificate | Check the development certificate and trust state; restart the browser after trusting it. |
| Port 7180 or 5180 is already in use | Stop your other development instance, or deliberately change the profile and use the new URLs. |
| `psql` is not recognized | Check its installation path or use pgAdmin; absence from `PATH` is not a server diagnosis. |
| A new dependency appears in a lock file | Inspect whether it is a direct or transitive dependency and why it was introduced. |

Do not solve compiler errors by deleting warnings, removing assertions, or replacing a real dependency with a mock without understanding the failure.

## 15. Make the first clean Git commit

This checkout had no `.git` directory when inspected. If that is still true after you implement the guide, initialize it yourself:

```powershell
git init -b main
git status --short
```

If it is already a repository by then, skip initialization and preserve its current branch/history.

Before staging:

- confirm `.vs`, `bin`, `obj`, secret files, and database exports are ignored;
- confirm generated `packages.lock.json` files are included;
- choose a license yourself and add `LICENSE` before public release;
- add `SECURITY.md` with an actual private reporting method and the educational/non-production boundary;
- add `CONTRIBUTING.md` with the build/test commands and learning-ownership agreement;
- update the README implementation status only after your build/tests support the new claim;
- do not claim that this foundation is production-ready or that financial features exist.

Stage only the relevant files. One explicit initial example is:

```powershell
git add README.md docs .gitignore .editorconfig global.json Directory.Build.props Directory.Packages.props FintechBackendLab.slnx src tests
git diff --cached --check
git diff --cached --stat
git diff --cached
```

Review the actual staged content for secrets and unintended files. Stage `LICENSE`, `SECURITY.md`, and `CONTRIBUTING.md` separately once you have authored them. Then commit:

```powershell
git commit -m "chore: establish API foundation and functional tests"
```

For GitHub, create an empty public repository under your own account, without generating another README or license remotely. Confirm the repository URL before adding it as `origin`; then push your intended branch. No GitHub repository, remote, or deployment has been created by this guide.

**Publishing source is not deploying the API.** Keep this application local until its security and deployment prerequisites are implemented. A public repository must contain fictional data and no usable credentials.

## 16. Completion checklist and interview practice

### Evidence checklist

- [ ] I can explain every project, reference, package, and configuration file.
- [ ] The solution contains exactly the intended three projects.
- [ ] The Customers project does not reference the API or tests.
- [ ] The weather sample and empty template test are gone.
- [ ] Restore, Release build, functional tests, and formatting checks succeed.
- [ ] All nine functional test cases are discovered and pass.
- [ ] Scalar can call the diagnostic route during Development.
- [ ] Production-environment tests return `404` for Scalar/OpenAPI routes.
- [ ] Liveness is not described as database readiness.
- [ ] Package audit output has been reviewed.
- [ ] The staged diff contains no secrets or generated IDE/build output.
- [ ] A separate clean clone can restore in locked mode, build, and test after the commit is available.

### Explain these without looking at the guide

1. What is the difference between a solution, a project, a namespace, and an assembly?
2. Why is API → Customers acceptable, but Customers → API not acceptable?
3. Why are folders alone insufficient to enforce Clean Architecture?
4. What is registered by `AddControllers`, and what is mapped by `MapControllers`?
5. What does `AddApplicationPart` do, and what does it not do?
6. Why does this action not need `async`?
7. How do middleware and controller actions participate in one request?
8. What is the difference between OpenAPI and Scalar?
9. Why can a Release build still run with the Development environment?
10. Why does the test client use an HTTPS base address without testing a real certificate?
11. Why are these functional tests rather than isolated unit tests?
12. Why do package versions and SDK versions need separate controls?
13. What is the difference between liveness, database connectivity, and financial correctness?
14. What would need to change before exposing a customer endpoint to anyone else?

Small exercises:

- Rename one JSON property intentionally and observe which test fails. Explain why before restoring the intended contract.
- Temporarily remove `MapControllers` and predict the failure before running the tests.
- Add a test that verifies `/health/ready` is absent at this milestone.
- Write down where a customer registration use case, its EF mapping, and its authorization rule will belong.

Do not introduce deliberate failures into a shared or deployed environment. These are local learning exercises.

## 17. What to do immediately after this guide

Stop after the foundation and review it before adding business features. Continue through the [delivery roadmap](../roadmap.md): finish the HTTP fundamentals, establish PostgreSQL persistence, then build the domain and authenticated customer/wallet workflows in their documented order.

Useful next review request:

> Review my foundation files and test output. Do not edit application code. Check the project references, startup pipeline, environment behavior, package configuration, and tests. Ask me to explain the parts I should understand before adding PostgreSQL persistence.

**First task today:** complete Sections 3–4 yourself, then explain why each of the three projects exists. The rest of this guide gives you the full route to the first runnable milestone; you do not need to finish it in one sitting.
