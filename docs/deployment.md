# Carmine People — Deployment (Phase 0)

## Environments

| Environment | Purpose | Data |
|---|---|---|
| Development | Local machine (this repo) | Seed data, verbose logs |
| Staging | Azure, pre-prod validation | Anonymized/synthetic data |
| Production | Azure, live | Real data, restricted access |

## Target Azure resources

- **Backend API** — Azure App Service (Linux, .NET 8) or Azure Container Apps
- **Database** — Azure SQL Database (General Purpose tier to start; scale as needed)
- **File storage** — Azure Blob Storage, private containers, SAS-token access
- **Admin app** — Azure Static Web Apps (Angular build output) or App Service
- **Secrets** — Azure Key Vault, referenced by App Service via managed identity
- **Push notifications** — Firebase project (separate from Azure, cross-cloud by design)
- **Monitoring** — Azure Application Insights (wired to Serilog sink)

## Local development setup

1. Install .NET 8 SDK
2. Install SQL Server Express (or Developer Edition) — `backend/src/CarminePeople.Api/appsettings.Development.json` connection string points at `localhost\SQLEXPRESS`; adjust it to your instance name
3. Install Node.js + `npm install -g @angular/cli`
4. Install Flutter SDK + Android Studio (for emulator/toolchain)
5. `dotnet ef database update` in `backend/src/CarminePeople.Api` to apply migrations + seed data
6. `dotnet run` (backend), `ng serve` (admin), `flutter run` (mobile)

## CI/CD (proposed)

- GitHub Actions (or Azure DevOps Pipelines) per top-level folder:
  - `backend`: build, run unit + integration tests, `dotnet ef migrations bundle`, deploy to App Service
  - `admin`: `ng build --configuration production`, deploy to Static Web Apps
  - `mobile`: `flutter build apk` (and `flutter build ios` once iOS is in scope), artifact upload / store deployment as a later phase
- Migrations applied as an explicit pipeline step before new API version goes live (never auto-migrate on app startup in production)

## Release flow

`feature branch -> PR -> CI (build+test) -> merge to main -> auto-deploy to Staging -> manual approval -> deploy to Production`

## Configuration & secrets per environment

- `appsettings.{Environment}.json` for non-secret config
- Connection strings, JWT signing key, Firebase service account, Azure Storage
  keys — all injected via environment variables / Key Vault references, never checked in

## Rollback

- App Service deployment slots (staging slot -> swap) for near-zero-downtime rollback on backend
- EF Core migrations should be written to be reversible where practical; irreversible
  migrations (data-destructive) require an explicit sign-off step in the PR

## Open questions for approval

- Confirm Azure subscription/tenant and resource-group naming convention
- Confirm CI/CD tool (GitHub Actions vs Azure DevOps)
- Confirm target App Service pricing tier for Staging vs Production
