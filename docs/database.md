# Carmine People — Database Design (Phase 0)

PostgreSQL (Supabase-hosted), EF Core Code-First migrations via the Npgsql provider.
All tables include standard audit columns unless noted otherwise:

```
CreatedAt   timestamptz   not null
CreatedBy   uuid          (FK -> Employees.Id, nullable for system/seed)
UpdatedAt   timestamptz   null
UpdatedBy   uuid          null
IsDeleted   boolean       not null default false   -- soft delete
```

Primary keys: `uuid` (GUID), generated client-side (`Guid.NewGuid()`).

## Core tables

**Roles** — Id, Name (SuperAdmin/HRAdmin/Manager/Employee), Description
**Permissions** — Id, Code (e.g. `attendance.approve`), Description
**RolePermissions** — RoleId, PermissionId (allows permission overrides beyond fixed roles)

**Departments** — Id, Name, Description
**Designations** — Id, Title, DepartmentId
**Locations** — Id, Name, Address, Latitude, Longitude, GeofenceRadiusMeters

**Employees** — Id, EmployeeCode, FirstName, LastName, Email (unique), PasswordHash,
PhoneNumber, DepartmentId, DesignationId, LocationId, ManagerId (self-FK),
RoleId, DateOfJoining, DateOfBirth, EmploymentStatus (Active/Inactive/Terminated), ProfileImageUrl

**RefreshTokens** — Id, EmployeeId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenId, CreatedByIp

## Attendance

**Shifts** — Id, Name, Type (General/Morning/Evening/Night/Flexible), StartTime,
EndTime, GracePeriodMinutes, BreakRules (json or child table)

**EmployeeShifts** — EmployeeId, ShiftId, EffectiveFrom, EffectiveTo

**AttendanceRecords** — Id, EmployeeId, Date, CheckInAt, CheckInLat, CheckInLng,
CheckInAccuracyMeters, CheckOutAt, CheckOutLat, CheckOutLng, CheckOutAccuracyMeters,
Status (enum: Present/Absent/Late/HalfDay/Leave/Holiday/WeekOff/WFH/PendingRegularization),
WorkingMinutes (computed server-side, never accepted from client), LocationId

**Breaks** — Id, AttendanceRecordId, BreakStartAt, BreakEndAt, DurationMinutes (computed)

**RegularizationRequests** — Id, AttendanceRecordId, EmployeeId, Reason,
RequestedCheckInAt, RequestedCheckOutAt, Status (Pending/ManagerApproved/HRApproved/Rejected),
ApprovalTrail (child table: ApproverId, Action, ActedAt, Comment)

## Leave

**LeaveTypes** — Id, Name (Casual/Sick/Earned/Paid/Unpaid/WFH), IsPaid, RequiresAttachment, DefaultAnnualDays

**LeaveBalances** — EmployeeId, LeaveTypeId, Year, Available, Used, Pending

**LeaveApplications** — Id, EmployeeId, LeaveTypeId, StartDate, EndDate, DaysCount
(computed excluding weekends/holidays), Reason, AttachmentUrl,
Status (Pending/Approved/Rejected/Cancelled), ApprovalTrail (child table)

**Holidays** — Id, Name, Date, LocationId (nullable = applies to all), IsOptional

## Documents

**EmployeeDocuments** — Id, EmployeeId, DocumentType (OfferLetter/Policy/SalarySlip/IdProof/Other),
BlobStoragePath (private container, never a public URL), FileName, FileSizeBytes,
ContentType, UploadedByEmployeeId

## Announcements & Notifications

**Announcements** — Id, Title, Body, AudienceType (All/Department/Location/Specific),
AudienceFilterJson, PublishAt, ExpiresAt, CreatedByEmployeeId

**NotificationPreferences** — EmployeeId, Channel (Push/InApp/Email), Category, IsEnabled

**Notifications** — Id, EmployeeId, Title, Body, Type, IsRead, SentAt, DeepLinkPath

## Audit

**AuditLogs** (immutable, append-only) — Id, EmployeeId (actor), Action, EntityType,
EntityId, Timestamp, IpAddress, MetadataJson (before/after diff where applicable)

## Indexing notes

- `AttendanceRecords`: unique index on `(EmployeeId, Date)` to prevent duplicate check-ins per day
- `Employees.Email`: unique index
- `LeaveApplications`: index on `(EmployeeId, Status)`
- `AuditLogs`: index on `(EntityType, EntityId)` and `(Timestamp)`

## Migrations & seed data

EF Core Code-First migrations under `/database/migrations` (generated from
`CarminePeople.Infrastructure`). Seed data (development only, clearly flagged):
1 Super Admin, 1 HR Admin, 2 Managers, 5 Employees distributed across
HR/Technology/Marketing/Finance departments.

## Open questions for approval

- Confirm GUID vs bigint identity for PKs (GUID proposed for multi-client sync friendliness)
- Confirm whether break rules live as JSON column or normalized child table
- Confirm retention policy for AuditLogs (unbounded vs archival after N months)
