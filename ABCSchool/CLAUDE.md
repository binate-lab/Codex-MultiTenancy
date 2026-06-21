# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
# Build the solution
dotnet build TrajanTenancy.sln

# Run the API (startup project is WebApi)
dotnet run --project WebApi

# Run with a specific profile (http / https)
dotnet run --project WebApi --launch-profile https

# EF Core migrations — three separate DbContexts, all targeting the Infrastructure project
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi --context ApplicationDbContext --output-dir Migrations/ApplicationDb
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi --context TenantDbContext    --output-dir Migrations/TenantDb
dotnet ef migrations add <Name> --project Infrastructure --startup-project WebApi --context PkiDbContext       --output-dir Migrations/PkiDb

dotnet ef database update --project Infrastructure --startup-project WebApi --context ApplicationDbContext
dotnet ef database update --project Infrastructure --startup-project WebApi --context TenantDbContext
dotnet ef database update --project Infrastructure --startup-project WebApi --context PkiDbContext
```

There are no automated tests in this solution. Swagger UI is served at `/swagger` when the API is running.

## Architecture

The solution is named **TrajanTenancy** and follows Clean Architecture:

```
Domain          ← entities and enums only, no dependencies
Application     ← CQRS (MediatR), FluentValidation, Mapster; depends on Domain + Shared.Library
Infrastructure  ← EF Core, Identity, multi-tenancy, PKI, chat, messaging; depends on Application
WebApi          ← ASP.NET Core entry point; depends on Application + Infrastructure
ApiGateway      ← minimal stub, not yet wired to WebApi
```

Two external projects outside this repo are referenced by relative path:
- `../TrajanEcole.Shared.Library` — shared constants (`RoleConstants`, `TenancyConstants.DefaultPassword`, etc.)
- `../../../../SharedContracts/SharedContracts` — MassTransit message contracts

### Application layer (CQRS)

Features live under `Application/Features/{Domain}/` with `Commands/`, `Queries/`, and `Validations/` sub-folders. Each feature folder also holds DTOs, request/response types, and a service interface (e.g. `ICertificatService`, `IChatService`).

All controllers inherit `BaseApiController`, which lazily resolves `ISender` (MediatR) from the DI container. Every handler returns `IResponseWrapper` or `ResponseWrapper<T>` — never raw types.

A `ValidationPipelineBenaviour<,>` is registered as a MediatR pipeline behavior; any command/query with a matching `IValidator<T>` is validated automatically before the handler runs.

### Infrastructure layer

**Three DbContexts** sharing the same SQL Server database:
- `TenantDbContext` — Finbuckle tenant store (`Multitenancy.Tenants` table); no Identity tables
- `ApplicationDbContext : BaseDbContext` — per-tenant Identity + `Schools` + `SchoolMemberships`
- `PkiDbContext` — device certificate requests (`DemandesCertificats`) and issued certificates (`CertificatsAppareils`)

`BaseDbContext` extends `MultiTenantIdentityDbContext`, so it switches its connection string to `TenantInfo.ConnectionString` when one is present, enabling true per-tenant database isolation.

**Multi-tenancy** (Finbuckle.MultiTenant): tenant is resolved from the `tenant` HTTP header first, then from a JWT claim of the same name. `TrajanEcoleTenantInfo` holds the tenant-specific connection string, admin email, and subscription validity date.

**Identity & auth**: JWT bearer; permissions are stored as role claims (`ClaimConstants.Permission` type, value `Permission.{Feature}.{Action}`). The `[ShouldHavePermission]` attribute on controller actions enforces these. `SchoolPermissions` in `Infrastructure/Constants/PermissionContants.cs` is the single source of truth for all permissions. Root permissions (`IsRoot: true`) are only assigned on the root tenant.

**PKI / device certificates**: `CertificatAppareilMiddleware` validates a mutual-TLS client certificate (or `X-Client-Cert` header from a reverse proxy) against `PkiDbContext` on every non-exempt request. Disable for local dev with `PkiSettings:ValiderCertificatAppareil: false` in `appsettings.Development.json`. The CA key/cert lives in `WebApi/certs/`.

**Messaging**: MassTransit over RabbitMQ. ABCSchool is **publisher-only** — no consumers are registered here. Contracts live in the external `SharedContracts` project.

**Chat**: Anthropic SDK (`Anthropic` NuGet package). `ChatService` wraps the API; model and token limits are configured in `appsettings.json` under `ChatSettings`. The API key must be set there or via user secrets.

### Startup / seeding

On startup, `AddDatabaseInitializerAsync` runs:
1. `ITenantDbSeeder.InitializeDatabaseAsync` — migrates `TenantDbContext`, then for each tenant migrates `ApplicationDbContext` and seeds default roles (`Admin`, `Basic`) with permissions and an admin user whose credentials come from `TrajanEcoleTenantInfo`.
2. Migrates `PkiDbContext`.

### Naming conventions

Domain and Application code uses **French** for business terms (e.g. `Ecole`, `Demande`, `Certificat`, `Appareil`, `Soumettre`, `Approuver`, `Révoquer`). Infrastructure and WebApi glue code is in English. Keep this split consistent.
