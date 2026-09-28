# Carmine People — Testing Strategy (Phase 0)

## Backend

- **Unit tests** (`CarminePeople.UnitTests`): Application-layer handlers,
  validators, domain logic (working-hours calculation, leave day-count
  calculation, geofence check, permission evaluation) — isolated from EF Core/DB
- **Integration tests** (`CarminePeople.IntegrationTests`): full request pipeline
  against a test database (e.g. SQL Server in a container, or EF Core InMemory
  where acceptable), covering auth, attendance, breaks, leave, approvals, permissions

## Required coverage areas (from roadmap)

- Auth: login, refresh rotation, logout, forgot/reset password, `me` endpoint permission shape
- Attendance: check-in/out happy path, duplicate check-in rejection, geofence
  rejection, break start/end, WorkingMinutes computed correctly across midnight/night shifts
- Regularization: full employee -> manager -> HR approval chain, audit trail written
- Leave: application, weekend/holiday-aware day-count calculation, approval workflow,
  balance deduction/restoration on approve/reject/cancel
- Permissions: manager scoped to own reports, HR Admin scoped per module, Super Admin unrestricted
- Location validation: server never trusts client-asserted geofence result

## Explicit edge cases (from roadmap — must have dedicated tests)

- Duplicate check-in same day
- Forgotten checkout (no checkout recorded — how status/hours resolve)
- Midnight/overnight shift boundary (Night shift crossing calendar date)
- Timezone handling (employee location vs server vs UTC storage)
- GPS accuracy/spoofing edge cases (low accuracy, outside geofence, missing coordinates)
- Offline mobile handling — attendance must never be shown as confirmed before server ack

## Frontend

- **Admin (Angular)**: component/unit tests for guards (permission-based routing),
  key forms (employee create/edit, leave approval), and table filter/sort/pagination logic
- **Mobile (Flutter)**: widget tests for check-in/out flow states (loading/success/error/offline),
  unit tests for Riverpod providers handling attendance/leave state

## Test data

Seed data used for integration/manual testing is clearly marked
development-only and never deployed to Staging/Production with real-looking values.

## Definition of done (per phase)

- All new endpoints/flows have unit + integration test coverage for the areas above
- No known critical bug open
- `dotnet test`, Angular `ng test`, and `flutter test` all pass in CI before merge

## Open questions for approval

- Confirm integration test DB strategy (Testcontainers SQL Server vs EF Core InMemory vs LocalDB)
- Confirm minimum coverage threshold to enforce in CI, if any
