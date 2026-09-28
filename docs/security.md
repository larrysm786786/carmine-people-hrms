# Carmine People — Security (Phase 0)

## Transport & auth

- HTTPS enforced everywhere (HSTS in production)
- JWT access tokens (short-lived, ~15 min) + refresh tokens (rotated on every use, revocable)
- Refresh tokens stored hashed in `RefreshTokens` table; old token invalidated when rotated
- Passwords hashed with a modern adaptive algorithm (BCrypt/Argon2), never reversible encryption

## Authorization

- Policy-based authorization on top of roles: permission checks (`attendance.approve`,
  `employee.delete`, etc.) evaluated per-request, not hardcoded role string checks
- Manager scope restricted to their reporting employees; enforced server-side on every query, not just UI hiding
- Super Admin actions (role/permission changes) themselves write to AuditLogs

## Input validation & injection protection

- FluentValidation on every incoming DTO in the Application layer
- EF Core parameterized queries only — no raw SQL string concatenation
- Angular's built-in sanitization for any dynamic HTML; no `innerHTML` binding of untrusted content
- Rate limiting on auth endpoints (login, forgot-password) to blunt brute force / credential stuffing
- CSRF: N/A for pure JWT bearer APIs (no cookie-based session), but any cookie-based
  flow (e.g. refresh token as httpOnly cookie) must carry CSRF protection

## Secrets

- No secrets in source control — connection strings, JWT signing keys, Firebase
  service account, Azure storage keys all via environment variables / Azure Key Vault
- Separate secrets per environment (Dev/Staging/Prod)

## File uploads (documents)

- Allowlist of content types (pdf, docx, jpg, png) and max size enforced server-side
- Files stored in a private Azure Blob container; access only via short-lived SAS URLs
- Virus/malware scan step before a document is marked available (flagged as future hardening item if not in Phase 1 scope)

## Location data integrity

- Client-supplied lat/lng/accuracy is *evidence*, not authority — server always
  re-validates against the assigned location's geofence radius before accepting a check-in/out
- Spoofed/mocked GPS is a known risk; flag as a Phase 8 hardening item (e.g. mock-location detection on mobile)

## Attendance/leave integrity

- WorkingMinutes, leave balance, approval state are always server-computed and
  server-owned. No endpoint accepts these values directly from a client.
- Duplicate check-in for the same employee/day is rejected at the database
  constraint level (unique index), not just application logic.

## Audit logging

- Immutable `AuditLogs` table: logins (success/failure), all CRUD on sensitive
  entities, approvals, role/permission changes — actor, action, entity, timestamp, IP, metadata
- Audit log writes never block on notification/email failures (fire in background where safe)

## Headers & general hardening

- Standard secure headers (X-Content-Type-Options, X-Frame-Options/frame-ancestors,
  Referrer-Policy, Content-Security-Policy on Angular admin app)
- CORS restricted to known client origins per environment (no wildcard `*` in production)

## Password policy

Enforced via `PasswordRuleExtensions.MustBeAStrongPassword()`, applied to both
employee creation and password reset: minimum 8 characters, at least one
uppercase letter, one lowercase letter, and one digit.

## Known accepted tradeoff: admin token storage

The Angular admin app stores its JWT access and refresh tokens in
`localStorage` (`token-storage.service.ts`), not an httpOnly cookie. This
means a successful XSS in the admin app could read both tokens, including
the longer-lived refresh token. Moving to httpOnly cookies would require
CSRF protection and a rework of the auth interceptor/CORS setup — a
deliberate architectural change, not a quick fix.

Accepted for now because: this is an internal HR back-office tool with a
small, known user base (not a public-facing consumer app), Angular's
built-in template sanitization already blocks the most common XSS vector
(no raw `innerHTML` binding of untrusted content anywhere in the app), and
the mobile app — which handles the same tokens for a much larger user base
— already uses `flutter_secure_storage` (OS keychain/keystore), not
localStorage-equivalent storage. Revisit if the admin app's user base or
threat model changes (e.g. becomes internet-facing to external partners).

## AI service layer

- AI service never connects to the database directly
- Every AI "tool call" is proxied through the normal backend API using the
  *requesting employee's* JWT/permission context — it can only see what that
  employee could see through the normal API

## Open questions for approval

- Confirm whether MFA is in scope for Phase 1 or a later hardening phase
- Confirm malware-scanning provider for document uploads (Azure Defender vs third-party)
