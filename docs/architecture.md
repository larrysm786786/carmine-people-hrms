# Carmine People — Architecture (Phase 0)

## 1. Overview

Carmine People is a white-labelable Employee Management & HRMS platform covering
auth, profiles, attendance, leave, holidays, shifts, documents, announcements,
notifications, approvals, HR admin, reports, dashboards, RBAC, audit logs,
location-based attendance, and a future AI assistant.

## 2. Repository layout (monorepo)

```
/mobile     Flutter app (Android primary, iOS-ready)
/admin      Angular admin/HR dashboard
/backend    ASP.NET Core Web API (Clean Architecture)
/database   SQL Server schema, EF Core migrations, seed scripts
/docs       architecture, database, api, security, deployment, ux-guidelines, testing
```

Each top-level folder is independently buildable/deployable but shares
contracts defined in `/docs/api.md` and `/database`.

## 3. Backend — Clean Architecture

```
backend/
  src/
    CarminePeople.Domain          Entities, enums, value objects, domain events. No dependencies.
    CarminePeople.Application     Use cases (CQRS-style handlers), DTOs, interfaces (IRepository, IEmailSender...), FluentValidation validators.
    CarminePeople.Infrastructure  EF Core DbContext, repository implementations, external services (Firebase, storage, Azure).
    CarminePeople.Api             Controllers, middleware, DI wiring, Swagger, API versioning, Serilog.
  tests/
    CarminePeople.UnitTests
    CarminePeople.IntegrationTests
```

Dependency rule: `Api -> Application -> Domain`, `Infrastructure -> Application/Domain`.
Domain never depends on Infrastructure or Api. Application depends only on
Domain and abstractions (interfaces), never on EF Core types directly in
handler signatures.

Cross-cutting:
- JWT bearer auth + refresh token rotation
- Policy-based authorization (permission claims, not just role names)
- Global exception-handling middleware -> consistent error envelope, no stack traces leaked
- Serilog structured logging (request id, user id, correlation id)
- API versioning under `/api/v1/...`
- Swagger/OpenAPI for all endpoints

## 4. Frontend clients

### Mobile (Flutter)
- State management: Riverpod
- Routing: GoRouter
- HTTP: Dio (with interceptors for JWT attach + refresh-on-401)
- Custom design system (no default Material look), dark mode via theme tokens
- Never trusts local calculations for attendance/leave/hours — always defers to backend response

### Admin (Angular)
- Angular + TypeScript, Angular Material used selectively (not wholesale)
- SaaS-dashboard quality UI: KPI cards, charts, data tables with search/filter/sort/pagination
- Route guards mirror backend permission model

## 5. Data flow principle

Server is always the source of truth for: attendance status/working hours,
leave balance, approval state, permissions. Frontend (mobile or admin) never
computes or optimistically confirms these — it displays what the backend
returns and shows pending/sync states while awaiting confirmation.

## 6. AI service layer (future)

A decoupled service (separate deployable, not inside `CarminePeople.Api`)
exposing permission-aware "tools" to an LLM. It never queries the database
directly — it calls backend APIs using the *requesting user's* permission
context, so it cannot leak another employee's private/salary/document data.

## 7. Cloud (Azure)

- API: Azure App Service (or Container Apps)
- Database: Azure SQL
- File storage: Azure Blob Storage (documents — private containers, SAS-token access, never public URLs)
- Admin/mobile static hosting: Azure Static Web Apps / App Service as applicable
- Secrets: Azure Key Vault, never committed to source
- Push notifications: Firebase Cloud Messaging (mobile), backend triggers via Firebase Admin SDK

## 8. Environments

`Development` (LocalDB/local SQL Server, seed data, verbose logging) ->
`Staging` (Azure, anonymized data) -> `Production` (Azure, secrets via Key Vault,
minimal logging verbosity, alerting).

## 9. Open questions for approval before Phase 1

- Confirm .NET target version (proposed: .NET 8 LTS)
- Confirm Angular version (proposed: latest stable Angular)
- Confirm whether iOS build/signing is in scope for Phase 1 or deferred
- Confirm Azure subscription/resource naming convention
