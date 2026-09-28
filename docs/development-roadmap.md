# Carmine People — Development Roadmap

This tracks build phases against the full spec (67 sections covering
Employee Management through Final Acceptance Criteria). Status reflects
what has been verified working end-to-end (build + tests +, where
applicable, live API/UI verification), not just "code exists."

## Phase 0 — Foundation

- Clean Architecture backend scaffold (Domain/Application/Infrastructure/Api)
- EF Core Code-First schema, SQL Server
- JWT auth + refresh token rotation, permission-claim authorization
- Flutter app scaffold (Riverpod, GoRouter, Dio) and Angular admin scaffold
- CI workflows for backend/admin/mobile

**Status: Done.**

## Phase 1 — Core HR features (Sections 28–48)

Employee management, organization (departments/designations/locations/shifts),
attendance (check-in/out, breaks, regularization), leave (types, apply,
approve, balances), holidays, documents, announcements, notifications,
reports, audit log, role/permission management, branding, AI assistant.

Includes hardening passes done alongside feature work:
- Security: secrets moved out of source control, fail-fast startup guard for
  placeholder secrets, rate limiting (auth/AI/global), file upload
  magic-byte validation
- AI assistant: defense-in-depth per-employee data scoping, independent of
  query filters
- Design system: semantic status colors (success/warning/danger/info) as a
  proper theme extension (mobile) / separate CSS custom-property namespace
  (admin), kept distinct from Material's brand-aliased error token

**Status: Done.**

## Phase 2 — Testing (Section 49)

- Unit tests: working-minutes calculator, geofence distance, leave day
  counting, shift timing (including the night-shift midnight edge case)
- Integration tests: real per-test-class SQL Server database, full HTTP
  round-trips — attendance (geofence, duplicate check-in, breaks), leave
  (apply/approve/cancel, balance math, overlap rejection), employee creation,
  notifications

**Status: Done.** 41 tests passing (20 unit + 21 integration).

## Phase 3 — Edge cases & robustness (Section 50)

- Night-shift midnight late-detection math (real bug, fixed)
- Overlapping leave application rejection
- Terminated/inactive employee blocked from check-in and leave apply (closes
  the window where a still-valid JWT issued before termination could be used)
- Company-timezone-aware "today" boundary via a shared `CompanyClock`
  service, applied consistently across attendance, dashboard, AI assistant,
  and the attendance reminder background job (previously each used raw UTC
  independently and could disagree with each other)
- Check-in race-condition handling (DB unique constraint + friendly error
  instead of an unhandled 500)
- Circular manager-reference prevention (self-manage, and longer cycles)

**Status: Done.**

## Phase 4 — UX polish (Sections 37–43, 51)

- Loading states: skeleton loaders (mobile), loading indicators (admin) —
  audited and completed across all screens, including previously-silent
  gaps (mobile profile screen, admin audit/organization/roles pages)
- Empty states: consistent "no data" messaging with icon/text across all
  list-type screens
- Error states: centralized offline-vs-server-error distinction, visible
  error message + Retry action instead of silent blank/zero-value fallback

**Status: Done.**

## Phase 5 — Timezone correctness (Section 52)

Company-configurable time zone (`BrandingSettings.TimeZoneId`), storage
always UTC, display/boundary-decisions converted via `CompanyClock`.

**Status: Done** (delivered as part of Phase 3/edge-cases work, since the
same root cause — inconsistent "today" boundaries — covered both).

## Phase 6 — Project hygiene (Section 53)

Repository structure audit: Clean Architecture layering confirmed correct;
EF Core Code-First confirmed as the intentional single source of schema
truth (no separate `/database` folder needed); stray local dev-session log
files removed from the repo root.

**Status: Done.**

## Phase 7 — Documentation (Section 54)

This roadmap, the product requirements doc, and the root `README.md` —
written after the fact to reflect the system as actually built and verified,
alongside the existing architecture/database/api/security/deployment/
testing/ux-guidelines docs from Phase 0.

**Status: Done.**

## Phase 8 — Environment configuration (Section 55)

Audited `.env.example` against every ASP.NET Core config key actually read
by the app (`Jwt:*`, `Cors:AllowedOrigins`, `DocumentStorage:RootPath`,
`Fcm:ServiceAccountJson`, `Ai:Anthropic:*`). Found and fixed a real gap:
several of these were documented in `.env.example` but never wired into
`docker-compose.yml`'s `environment:` block, so a Docker deployment would
silently ignore them and fall back to `appsettings.json` defaults —
document storage path and FCM push, in particular, would never pick up a
real value. All are now wired through.

**Status: Done.**

## Phase 9 — Seed data (Section 56)

`DevelopmentSeeder.cs` seeded roles/employees/org/leave-types/holidays but
nothing else, so a fresh database showed empty dashboards, empty attendance
history, an empty leave queue, and no announcements. Added a shift with
assignments, 5 working days of realistic attendance history (present/late/
absent mix), two leave applications (Pending and Approved, with correct
balance effects), and two announcements (one active, one expired). Verified
live against a dropped-and-reseeded local dev database.

**Status: Done.**

## Phase 10 — Initial screens checklist (Section 57)

Audited every mobile screen (16) and admin page (12) against the expected
v1 HRMS screen list. All present and routed. Found bonus screens beyond the
checklist: biometric app-lock, face enrollment, attendance regularization
(mobile); AI Insights (admin).

**Status: Done.** No gaps found.

## Phase 11 — Design quality (Section 58)

Audited typography, spacing, dark mode, icon consistency, and empty-state
polish across both apps. Found and fixed two real gaps: the mobile theme
had no deliberate `textTheme` (inherited Material3 defaults rather than a
designed type scale); the admin global stylesheet had a repeated
radius/shadow pattern across `dashboard.scss`/`ai-insights.scss` with no
shared token, so it wasn't actually reusable. Added a custom mobile type
scale and shared `--app-radius-*`/`--app-shadow-*`/`--app-space-*` CSS
custom properties, wired into the existing usages (zero visual change,
now tokenized).

**Status: Done.**

## Remaining

- Process/retrospective sections (59–67): development method, git workflow
  (not applicable — this repo is not yet under version control), code
  quality, database/security rules, final acceptance criteria — largely a
  verification pass against work already completed above, rather than new
  implementation.

## Known deferred items

- Leave applications spanning a calendar-year boundary (e.g. Dec 28 – Jan 3)
  draw the entire balance from the start date's year rather than splitting
  across both years' `LeaveBalance` rows. Rare in practice; flagged during
  the Section 50 edge-case audit and intentionally deferred rather than
  rushed.
- iOS build/signing not yet done (Android is the primary target).
