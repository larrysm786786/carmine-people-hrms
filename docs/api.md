# Carmine People — API Design (Phase 0)

Base path: `/api/v1/{module}`. All responses use a consistent envelope; no
internal stack traces are ever exposed to clients.

## Response envelope

Success:
```json
{ "success": true, "data": { }, "meta": { "page": 1, "pageSize": 20, "total": 137 } }
```

Error:
```json
{ "success": false, "error": { "code": "VALIDATION_ERROR", "message": "...", "details": [ ] } }
```

## Auth (`/api/v1/auth`)

- `POST /login` — email + password -> access token + refresh token
- `POST /refresh` — refresh token -> new access + refresh token (rotation, old token revoked)
- `POST /logout` — revokes refresh token
- `POST /forgot-password` — sends reset link/OTP
- `POST /reset-password` — token + new password
- `GET /me` — current authenticated employee profile + permissions

## Employees (`/api/v1/employees`)

- `GET /` — paginated, filterable (department, location, status), sortable
- `GET /{id}`
- `POST /` — HR Admin/Super Admin only
- `PUT /{id}`
- `DELETE /{id}` — soft delete
- `GET /{id}/documents`

## Departments / Designations / Locations

Standard CRUD under `/api/v1/departments`, `/api/v1/designations`, `/api/v1/locations`.
Locations include geofence radius; never accept client-asserted "inside geofence" flags.

## Attendance (`/api/v1/attendance`)

- `POST /check-in` — body: lat, lng, accuracy. Server validates against assigned
  location's geofence; rejects if outside radius or duplicate check-in for the day.
- `POST /check-out` — body: lat, lng, accuracy. Server computes WorkingMinutes.
- `POST /break-start` / `POST /break-end`
- `GET /` — paginated attendance records, filterable by employee/date range/status
- `GET /{id}`
- `POST /{id}/regularize` — employee submits regularization request
- `PUT /regularization/{id}/approve` — manager/HR step in approval chain
- `PUT /regularization/{id}/reject`

## Leave (`/api/v1/leaves`)

- `GET /types`
- `GET /balances?employeeId=`
- `POST /` — apply for leave (date range, type, attachment)
- `GET /` — paginated, filterable by employee/status/type
- `PUT /{id}/approve`
- `PUT /{id}/reject`
- `PUT /{id}/cancel`

## Holidays (`/api/v1/holidays`)

Standard CRUD, filterable by location/year.

## Shifts (`/api/v1/shifts`)

Standard CRUD + `POST /assign` (employee <-> shift mapping with effective dates).

## Notifications (`/api/v1/notifications`)

- `GET /` — current employee's notifications, paginated
- `PUT /{id}/read`
- `GET /preferences` / `PUT /preferences`
- Server-triggered push via Firebase Admin SDK (no client-exposed send endpoint except for admin-initiated announcements)

## Announcements (`/api/v1/announcements`)

Standard CRUD (HR Admin/Super Admin) with audience targeting; `GET /` for
employee-facing feed filtered to the caller's audience.

## Documents (`/api/v1/documents`)

- `POST /upload` — validated type/size, stored in private Azure Blob container
- `GET /{id}/download` — returns short-lived SAS URL, never a permanent public link
- `DELETE /{id}` — soft delete

## Reports (`/api/v1/reports`)

- `GET /attendance` / `GET /leave` / `GET /employee` / `GET /late` / `GET /working-hours`
- Query params: date range, department, location, employee
- `GET /{report}/export?format=csv|xlsx|pdf`

## Audit (`/api/v1/audit`)

- `GET /` — Super Admin only, paginated, filterable by entity/action/employee/date range

## Versioning & docs

- URL-segment versioning (`/api/v1/...`); breaking changes ship as `/api/v2/...`
- Swagger/OpenAPI generated and served at `/swagger` in non-production environments

## Open questions for approval

- Confirm pagination defaults (proposed: page=1, pageSize=20, max 100)
- Confirm export generation is synchronous or queued for large reports
- Confirm whether AI assistant gets its own `/api/v1/ai/*` gateway or a fully separate host
