# Carmine People — Deployment (Phase 0)

## Environments

| Environment | Purpose | Data |
|---|---|---|
| Development | Local machine (this repo) | Seed data, verbose logs |
| Staging | Render, pre-prod validation | Anonymized/synthetic data |
| Production | Render, live | Real data, restricted access |

## Target resources

- **Backend API** — Render web service (Docker runtime, `backend/Dockerfile`), auto-deployed on push to `main` — see `render.yaml`
- **Database** — Supabase (hosted PostgreSQL)
- **File storage** — local disk (`DocumentStorage__RootPath`) to start; swap for Supabase Storage or S3-compatible storage if scale requires it
- **Admin app** — Render web service (Docker runtime, `admin/Dockerfile`, nginx-served build) — see `render.yaml`
- **Secrets** — Render environment variables (dashboard-managed, `sync: false` in `render.yaml`), never checked in
- **Push notifications** — Firebase project (cross-cloud by design)
- **Monitoring** — Serilog console sink (Render captures stdout/stderr logs)

## Local development setup

1. Install .NET 8 SDK
2. Install PostgreSQL (or run `docker compose up postgres`) — `backend/src/CarminePeople.Api/appsettings.Development.json` connection string points at `localhost:5432`; adjust it to your instance
3. Install Node.js + `npm install -g @angular/cli`
4. Install Flutter SDK + Android Studio (for emulator/toolchain)
5. `dotnet ef database update` in `backend/src/CarminePeople.Api` to apply migrations + seed data
6. `dotnet run` (backend), `ng serve` (admin), `flutter run` (mobile)

## CI/CD

- GitHub Actions per top-level folder:
  - `backend`: build, run unit + integration tests against a Postgres service container (`.github/workflows/backend-ci.yml`)
  - `admin`: `ng build --configuration production` (`.github/workflows/admin-ci.yml`)
  - `mobile`: `flutter build apk` (and `flutter build ios` once iOS is in scope), artifact upload / store deployment as a later phase
- Render connects directly to the GitHub repo and auto-deploys both web services (`render.yaml`) on push to `main` — no separate deploy pipeline step needed for backend/admin
- Migrations are applied as an explicit step (`dotnet ef database update` against the Supabase connection string) before a new API version goes live — never auto-migrate on app startup in production

## Release flow

`feature branch -> PR -> CI (build+test) -> merge to main -> Render auto-deploys backend + admin`

## Configuration & secrets per environment

- `appsettings.{Environment}.json` for non-secret config
- Connection strings, JWT signing key, Firebase service account — all injected via
  Render environment variables, never checked in

## Rollback

- Render supports rolling back a web service to a previous successful deploy from the dashboard
- EF Core migrations should be written to be reversible where practical; irreversible
  migrations (data-destructive) require an explicit sign-off step in the PR

## Open questions for approval

- Confirm Supabase project/org and Postgres plan (free tier to start)
- Confirm Render plan for Staging vs Production (free tier sleeps on inactivity — fine
  for Staging, consider a paid plan for Production)
