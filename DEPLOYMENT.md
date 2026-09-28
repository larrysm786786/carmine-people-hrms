# Running Carmine People locally with Docker

This mirrors the target Azure topology from `docs/deployment.md` (App Service/Container
Apps + Azure SQL + Static Web Apps) using local containers, so the same Dockerfiles
carry forward to Azure Container Apps/App Service without changes.

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
- SQL Server: localhost:1433 (sa / `SQL_SA_PASSWORD` from `.env`)

## Applying migrations

Per `docs/deployment.md`, this project never auto-migrates on startup outside
Development/Testing. Apply migrations explicitly as an release step:

```
cd backend
dotnet ef database update \
  --project src/CarminePeople.Infrastructure \
  --startup-project src/CarminePeople.Api \
  --connection "Server=localhost,1433;Database=CarminePeopleDb;User Id=sa;Password=<SQL_SA_PASSWORD>;TrustServerCertificate=True"
```

## CI

- `.github/workflows/backend-ci.yml` — builds the backend, runs unit tests, runs
  integration tests against a SQL Server service container
- `.github/workflows/admin-ci.yml` — builds the Angular admin app
- `.github/workflows/mobile-ci.yml` — analyzes, tests, and builds a debug APK for the
  Flutter app

## Path to Azure

The Dockerfiles here are what `backend`/`admin` App Service or Container Apps
deployments would build directly — no separate "cloud" Dockerfile is needed. What
changes for Azure (per `docs/deployment.md`) is only *where* config comes from:

- `ConnectionStrings__DefaultConnection` → Azure SQL connection string via Key Vault reference
- `Jwt__SigningKey` → Key Vault reference, never an App Service plain setting
- SQL Server container → Azure SQL Database
- nginx-served admin container → Azure Static Web Apps (or keep the same container on
  App Service if a static host isn't provisioned yet)
