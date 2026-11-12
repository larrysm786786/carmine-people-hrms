# Carmine People

A white-labelable Employee Management & HRMS platform: attendance, leave, holidays,
shifts, documents, announcements, notifications, approvals, HR admin, reports,
dashboards, RBAC, audit logs, location-based (geofenced) attendance, and an
AI HR assistant.

## Stack

| Layer | Technology |
|---|---|
| Mobile | Flutter (Android primary, iOS-ready), Riverpod, GoRouter, Dio |
| Admin dashboard | Angular + TypeScript, Angular Material (selective use) |
| Backend API | ASP.NET Core Web API, Clean Architecture, EF Core Code-First |
| Database | PostgreSQL (Supabase-hosted) |
| Cloud | Render (Docker web services for backend + admin, GitHub-connected auto-deploy) |
| Notifications | Firebase Cloud Messaging |
| AI | Decoupled AI service layer (Anthropic), never queries the DB directly |

## Repository layout

```
/mobile     Flutter app
/admin      Angular admin/HR dashboard
/backend    ASP.NET Core Web API (Clean Architecture: Domain/Application/Infrastructure/Api)
/docs       architecture, database design, API reference, security, deployment, UX guidelines, testing
```

See [docs/architecture.md](docs/architecture.md) for the full architecture, and
[docs/product-requirements.md](docs/product-requirements.md) /
[docs/development-roadmap.md](docs/development-roadmap.md) for what this system
does and how it was built.

## Running locally

**Backend:**
```
cd backend/src/CarminePeople.Api
dotnet run --urls "http://localhost:5083"
```
Port 5083 is what the admin dashboard (`api-config.ts`) and the mobile debug build
expect. Applies EF Core migrations and seeds development data automatically in the
`Development` environment. The connection string in `appsettings.Development.json`
points at a local PostgreSQL instance (`localhost:5432`); change it if your Postgres
instance uses a different host/port/credentials, or point it at a Supabase project's
connection string. See [docs/database.md](docs/database.md) for schema and seed details.

**Admin dashboard:**
```
cd admin
npm install
npm start
```
Serves at `http://localhost:4200`, calling the backend at the URL configured in
`admin/src/app/core/services/api-config.ts`.

**Mobile:**
```
cd mobile
flutter pub get
flutter run
```
Set the backend base URL in `mobile/lib/core/network/api_client.dart` — use your
machine's LAN IP (not `localhost`) when testing on a real device, or `10.0.2.2`
for the Android emulator.

**Everything via Docker** (mirrors the Azure topology): see
[DEPLOYMENT.md](DEPLOYMENT.md).

## Development seed accounts

`DevelopmentSeeder.cs` seeds a SuperAdmin, an HRAdmin, two Managers, and five
Employees automatically when the backend starts against an empty database in
the `Development` environment. Never used in Staging/Production. See the
seeder source for the generated email addresses, and set your own password
via `IPasswordHasher` rather than relying on a documented default.

## Testing

```
cd backend
dotnet test
```

Runs unit tests (pure calculators — working minutes, geofence distance, leave
day counting, shift timing) and integration tests (real SQL Server test
database per test class, full HTTP round-trips through the actual API). See
[docs/testing.md](docs/testing.md).

## Documentation index

- [docs/architecture.md](docs/architecture.md) — system architecture, layering rules, data-flow principles
- [docs/database.md](docs/database.md) — schema design
- [docs/api.md](docs/api.md) — API reference
- [docs/security.md](docs/security.md) — auth, permissions, secret handling
- [docs/deployment.md](docs/deployment.md) — Azure deployment
- [docs/testing.md](docs/testing.md) — test strategy and conventions
- [docs/ux-guidelines.md](docs/ux-guidelines.md) — design system, UX principles
- [docs/product-requirements.md](docs/product-requirements.md) — feature scope and requirements
- [docs/development-roadmap.md](docs/development-roadmap.md) — build phases and current status
