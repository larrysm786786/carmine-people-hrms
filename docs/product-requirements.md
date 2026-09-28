# Carmine People — Product Requirements

## Purpose

Carmine People is a white-labelable HRMS covering the day-to-day employee
lifecycle: attendance, leave, holidays, documents, and HR administration,
accessed via a Flutter mobile app (employees) and an Angular admin dashboard
(HR/managers), backed by a single ASP.NET Core API.

## Roles

| Role | Scope |
|---|---|
| SuperAdmin | Full system control, including branding and role/permission management |
| HRAdmin | Manages employees, attendance, leave, holidays, documents, announcements, org structure |
| Manager | Views their direct reports' attendance/leave, approves/rejects their requests |
| Employee | Self-service: check in/out, apply for leave, view own records/documents |

Authorization is permission-claim based (e.g. `employee.manage`, `leave.approve`),
not just role-name checks — a role is a named bundle of permissions, editable
by a SuperAdmin without a code change.

## Feature scope

### Employee Management
Create/update/deactivate employee records; org assignment (department,
designation, location, manager, role); self-service profile updates (contact
info, emergency contact, profile photo) separate from HR-only fields
(department/role/manager, which require `employee.manage`).

### Organization
Departments, designations, locations (each with GPS coordinates + geofence
radius), shifts (day/night, grace period, break rules), and employee-to-shift
assignments with effective-dated history.

### Attendance
GPS-geofenced check-in/check-out; optional face-verification as an additional
layer on top of (never a replacement for) the geofence check; break
start/end; attendance regularization requests (Employee → Manager → HR
approval chain) for correcting a missed or wrong check-in/out; a background
job reminds employees who haven't checked in after their shift's grace period.

### Leave
Configurable leave types with attachment requirements; leave application with
working-day counting (excludes weekends and holidays); two-stage approval
(Manager → HR, matching the Attendance regularization chain); balance
tracking (Available / Pending / Used) per employee per leave type per year;
overlapping-application prevention; cancellation restores pending balance.

### Holidays
Company-wide and location-specific holiday calendar, optional holidays,
factored into leave working-day counting.

### Documents
Employee document upload (ID proofs, certificates, profile photos) with
content-type allowlisting AND magic-byte verification (a client-declared
MIME type is never trusted alone); private storage, access via short-lived
signed download URLs, never public.

### Announcements
Company-wide announcements visible to all employees, HR-authored.

### Notifications
In-app + push (Firebase Cloud Messaging) notifications for leave decisions,
attendance reminders, regularization decisions, announcements; per-category
notification preferences.

### HR Admin / Dashboard
KPI dashboard (present/absent/late/on-leave today, attendance trend, leave
trend, department headcount, working-hours trend, pending approvals feed,
recent hires, recent announcements) — all "today"/date-boundary figures
resolve against the company's configured time zone, not raw server UTC.

### Reports
Filterable, exportable (CSV/Excel) attendance and leave reports by date
range, department, employee, and status.

### Roles & Permissions
SuperAdmin-managed role/permission matrix; new permissions are additive and
require an explicit grant, never implicit.

### Audit Log
Every state-changing action (employee changes, approvals, document
upload/delete, login/logout) is logged with actor, action, entity, and
timestamp; queryable and filterable by HR/SuperAdmin.

### Branding
White-label configuration: company name, app name, logo, brand colors,
gradient, and company time zone — editable by SuperAdmin, applied live across
mobile and admin.

### AI Assistant
A permission-scoped chat assistant. Employee-facing: answers questions about
the *asking employee's own* leave balance, today's attendance, and upcoming
holidays only — every row fetched for the LLM's context is asserted to
belong to the requesting employee before use, as a defense-in-depth
boundary independent of the query filters. HR-facing: aggregate,
anonymized org-wide stats only (headcounts, counts), never individual
employee-level detail.

## Non-functional requirements

- **Server is the source of truth.** Neither mobile nor admin computes or
  optimistically confirms attendance status, working hours, leave balance,
  or approval state — they display what the backend returns.
- **Offline-aware.** Network failures are distinguished from server-returned
  errors and never silently reported as success; the mobile app shows a
  clear "you're offline" message rather than a false confirmation.
- **Rate limited.** Auth and AI endpoints have dedicated stricter limits; a
  global per-IP fallback limiter covers everything else.
- **Secrets never in source control.** Local dev uses `dotnet user-secrets`;
  non-Development environments fail fast at startup if a required secret is
  still a placeholder value.
- **Consistent error envelope.** Every API error returns
  `{ success: false, error: { code, message }, traceId }` — no leaked stack
  traces.

## Out of scope (current phase)

- iOS build/signing (Android is primary; the Flutter codebase is iOS-ready
  but not yet built/signed for release)
- Payroll, benefits administration, recruitment/ATS
