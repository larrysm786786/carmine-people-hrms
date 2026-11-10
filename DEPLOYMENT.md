# Running Carmine People locally with Docker

This mirrors the target Render topology from `docs/deployment.md` (Docker web service +
Supabase Postgres + static site) using local containers, so the same Dockerfiles carry
forward to Render without changes.

## Prerequisites

- Docker Desktop
- Copy `.env.example` to `.env` and fill in real values:
  ```
  cp .env.example .env
  ```

## Run everything

```
docker compose up --build
```

- Backend API: http://localhost:5080 (Swagger only in Development builds — this compose
  file runs `ASPNETCORE_ENVIRONMENT=Production`, so apply migrations explicitly, see below)
- Admin dashboard: http://localhost:4200
- Postgres: localhost:5432 (postgres / `POSTGRES_PASSWORD` from `.env`)

## Applying migrations

Per `docs/deployment.md`, this project never auto-migrates on startup outside
Development/Testing. Apply migrations explicitly as a release step:

```
cd backend
dotnet ef database update \
  --project src/CarminePeople.Infrastructure \
  --startup-project src/CarminePeople.Api \
  --connection "Host=localhost;Port=5432;Database=CarminePeopleDb;Username=postgres;Password=<POSTGRES_PASSWORD>"
```

Against Supabase, swap the `--connection` value for the connection string from the
Supabase dashboard (Project Settings > Database > Connection string).

## CI

- `.github/workflows/backend-ci.yml` — builds the backend, runs unit tests, runs
  integration tests against a Postgres service container
- `.github/workflows/admin-ci.yml` — builds the Angular admin app
- `.github/workflows/mobile-ci.yml` — analyzes, tests, and builds a debug APK for the
  Flutter app

## Path to Render + Supabase

The Dockerfiles here are what Render's backend web service builds directly — no
separate "cloud" Dockerfile is needed. See `render.yaml` at the repo root for the
Render Blueprint. What changes for a deployed environment (per `docs/deployment.md`)
is only *where* config comes from:

- `ConnectionStrings__DefaultConnection` → Supabase Postgres connection string, set as a
  Render environment variable (never committed)
- `Jwt__SigningKey` → a Render environment variable (`sync: false` in `render.yaml`),
  never committed
- Local Postgres container → Supabase-hosted Postgres
- nginx-served admin container → Render static site (or keep the same container as a
  Render Docker web service if you'd rather not switch to a static build)
