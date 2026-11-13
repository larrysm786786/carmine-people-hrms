using System.Text.Json;
using CarminePeople.Application.Attendance.DTOs;
using CarminePeople.Application.Common.Face;
using CarminePeople.Application.Common.Geo;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Domain.Constants;
using CarminePeople.Domain.Entities;
using CarminePeople.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Attendance.Services;

public class AttendanceService
{
    private const double MaxAcceptableGpsAccuracyMeters = 100;

    private readonly IApplicationDbContext _db;
    private readonly IAuditLogger _auditLogger;
    private readonly NotificationService _notificationService;
    private readonly CompanyClock _clock;

    public AttendanceService(IApplicationDbContext db, IAuditLogger auditLogger, NotificationService notificationService, CompanyClock clock)
    {
        _db = db;
        _auditLogger = auditLogger;
        _notificationService = notificationService;
        _clock = clock;
    }

    public async Task<Result<AttendanceRecordDto>> CheckInAsync(Guid employeeId, CheckInRequest request, CancellationToken ct)
    {
        var employee = await _db.Employees.Include(e => e.Location).FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return Result<AttendanceRecordDto>.Fail("NOT_FOUND", "Employee not found.");
        if (employee.EmploymentStatus != EmploymentStatus.Active)
        {
            // Login is already blocked for non-Active employees (AuthService), but a JWT
            // issued before termination stays valid until it expires — this catches that window.
            return Result<AttendanceRecordDto>.Fail("EMPLOYEE_INACTIVE", "Your employment status does not permit check-in.");
        }

        // Office WiFi and GPS geofence are two independent ways to prove presence — either one
        // is enough, not both. A location with no SSID configured has no WiFi path at all
        // (IsAllowed would trivially return true for "not restricted", which would wrongly
        // satisfy an OR here, so that path is only considered when an SSID is actually set).
        var isOnOfficeWifi = !string.IsNullOrWhiteSpace(employee.Location.OfficeWifiSsid)
            && OfficeWifiMatcher.IsAllowed(employee.Location.OfficeWifiSsid, request.WifiSsid);

        if (!isOnOfficeWifi)
        {
            // Not confirmed via WiFi — fall back to GPS. A reading with >100m of reported
            // uncertainty can't reliably confirm presence within any reasonably-sized office
            // geofence — reject before even comparing distance, since a low-accuracy fix
            // passing the geofence check would be a false positive, not a real confirmation.
            if (request.AccuracyMeters > MaxAcceptableGpsAccuracyMeters)
            {
                return Result<AttendanceRecordDto>.Fail("POOR_GPS_ACCURACY", "Your location signal is too imprecise to check in. Connect to the office WiFi, or move to an open area and try again.");
            }

            var distance = GeoDistanceCalculator.DistanceMeters(
                request.Latitude, request.Longitude, employee.Location.Latitude, employee.Location.Longitude);
            if (distance > employee.Location.GeofenceRadiusMeters)
            {
                return Result<AttendanceRecordDto>.Fail("OUTSIDE_GEOFENCE", "Check-in location is outside the allowed geofence. Connect to the office WiFi, or move closer, and try again.");
            }
        }

        var today = await _clock.TodayAsync(ct);
        var existing = await _db.AttendanceRecords.Include(a => a.Employee).FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);
        if (existing is not null)
        {
            if (existing.IsCheckedIn)
            {
                return Result<AttendanceRecordDto>.Fail("DUPLICATE_CHECK_IN", "Attendance already recorded for today.");
            }

            // Re-entering the office after an earlier check-out today (e.g. back from lunch,
            // a vendor visit). CheckInAt must stay the day's original first check-in — only
            // the live "currently on-site" flag flips back on.
            existing.IsCheckedIn = true;
            await _db.SaveChangesAsync(ct);
            return Result<AttendanceRecordDto>.Ok(ToDto(existing));
        }

        // Face verification is an optional extra layer on top of the mandatory GPS geofence
        // check above, never a replacement for it. A mismatch or an unenrolled employee
        // silently falls back to a normal GPS check-in rather than failing the request —
        // HR can review unverified check-ins later via the FaceVerified flag.
        var faceVerified = false;
        if (request.FaceEmbedding is { Length: > 0 } && !string.IsNullOrEmpty(employee.FaceEmbedding))
        {
            var reference = JsonSerializer.Deserialize<float[]>(employee.FaceEmbedding);
            if (reference is not null && FaceEmbeddingMatcher.IsMatch(reference, request.FaceEmbedding))
            {
                faceVerified = true;
            }
        }

        var checkInAt = DateTime.UtcNow;

        // Late warning: compare against the employee's currently-effective shift (start time
        // + grace period). No shift assigned means there's nothing to be late against, so the
        // employee is never flagged in that case.
        var currentShift = await _db.EmployeeShifts
            .Include(es => es.Shift)
            .Where(es => es.EmployeeId == employeeId && es.EffectiveFrom <= checkInAt && (es.EffectiveTo == null || es.EffectiveTo >= checkInAt))
            .OrderByDescending(es => es.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        var isLate = false;
        if (currentShift is not null)
        {
            var graceDeadline = ShiftTimingCalculator.ResolveGraceDeadline(currentShift.Shift, checkInAt);
            isLate = checkInAt > graceDeadline;
        }

        // Cyclical late policy: the first N late arrivals this month (N = company-configurable
        // BrandingSettings.FreeLatesBeforeHalfDay) are a free warning (Status stays Late), the
        // next is a half-day, then the cycle repeats for the rest of the month.
        var status = AttendanceStatus.Present;
        if (isLate)
        {
            var freeLatesBeforeHalfDay = (await _db.BrandingSettings.FirstOrDefaultAsync(ct))?.FreeLatesBeforeHalfDay ?? 2;

            var counter = await _db.EmployeeMonthlyLateCounters.FirstOrDefaultAsync(
                c => c.EmployeeId == employeeId && c.Year == today.Year && c.Month == today.Month, ct);
            if (counter is null)
            {
                counter = new EmployeeMonthlyLateCounter { EmployeeId = employeeId, Year = today.Year, Month = today.Month };
                _db.EmployeeMonthlyLateCounters.Add(counter);
            }
            counter.LateCount++;

            status = AttendanceLatePolicyCalculator.IsHalfDayOccurrence(counter.LateCount, freeLatesBeforeHalfDay)
                ? AttendanceStatus.HalfDay
                : AttendanceStatus.Late;
        }

        var record = new AttendanceRecord
        {
            EmployeeId = employeeId,
            Date = today,
            CheckInAt = checkInAt,
            CheckInLat = request.Latitude,
            CheckInLng = request.Longitude,
            CheckInAccuracyMeters = request.AccuracyMeters,
            CheckInDeviceInfo = request.DeviceInfo,
            CheckInMethod = faceVerified ? CheckInMethod.Face : CheckInMethod.Gps,
            FaceVerified = faceVerified,
            LocationId = employee.LocationId,
            IsCheckedIn = true,
            Status = status
        };
        _db.AttendanceRecords.Add(record);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Two simultaneous check-in requests both pass the existing-record check above
            // before either commits; the unique index on (EmployeeId, Date) catches the loser
            // at the DB level. Translate that into the same friendly error instead of a 500.
            return Result<AttendanceRecordDto>.Fail("DUPLICATE_CHECK_IN", "Attendance already recorded for today.");
        }

        if (request.FaceEmbedding is { Length: > 0 })
        {
            await _auditLogger.LogAsync(employeeId, faceVerified ? "attendance.face_verified" : "attendance.face_mismatch", "AttendanceRecord", record.Id.ToString(), null, ct);
        }

        if (isLate)
        {
            var (title, body) = status == AttendanceStatus.HalfDay
                ? ("Half-day marked", $"You checked in at {checkInAt:HH:mm}. This is your 3rd late arrival this cycle, so today is marked as a half-day.")
                : ("Late check-in", $"You checked in at {checkInAt:HH:mm}, after your shift's grace period. Two more late arrivals this cycle will mark a half-day.");
            await _notificationService.NotifyAsync(employeeId, title, body, "attendance", $"/attendance/{record.Id}", ct);
        }

        record.Employee = employee;
        return Result<AttendanceRecordDto>.Ok(ToDto(record));
    }

    public async Task<Result<AttendanceRecordDto>> CheckOutAsync(Guid employeeId, CheckOutRequest request, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var record = await _db.AttendanceRecords.Include(a => a.Breaks).Include(a => a.Employee)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);

        if (record is null || record.CheckInAt is null)
        {
            return Result<AttendanceRecordDto>.Fail("NOT_CHECKED_IN", "No check-in found for today.");
        }
        if (!record.IsCheckedIn)
        {
            return Result<AttendanceRecordDto>.Fail("ALREADY_CHECKED_OUT", "Already checked out for today.");
        }
        if (record.Breaks.Any(b => b.BreakEndAt is null))
        {
            return Result<AttendanceRecordDto>.Fail("BREAK_IN_PROGRESS", "Cannot check out while a break is still in progress.");
        }

        // CheckOutAt always advances to the latest check-out — as long as the employee keeps
        // re-entering the office today (see CheckInAsync), this value keeps moving forward
        // until the final check-out of the day, which is what ends up persisted.
        record.CheckOutAt = DateTime.UtcNow;
        record.CheckOutLat = request.Latitude;
        record.CheckOutLng = request.Longitude;
        record.CheckOutAccuracyMeters = request.AccuracyMeters;
        record.IsCheckedIn = false;

        var totalBreakMinutes = record.Breaks.Sum(b => b.DurationMinutes ?? 0);
        record.WorkingMinutes = WorkingMinutesCalculator.Calculate(record.CheckInAt.Value, record.CheckOutAt.Value, totalBreakMinutes);

        await _db.SaveChangesAsync(ct);
        return Result<AttendanceRecordDto>.Ok(ToDto(record));
    }

    public async Task<Result<BreakDto>> StartBreakAsync(Guid employeeId, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var record = await _db.AttendanceRecords.Include(a => a.Breaks)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);

        if (record is null || record.CheckInAt is null || !record.IsCheckedIn)
        {
            return Result<BreakDto>.Fail("INVALID_STATE", "Must be checked in and not checked out to start a break.");
        }
        if (record.Breaks.Any(b => b.BreakEndAt is null))
        {
            return Result<BreakDto>.Fail("BREAK_IN_PROGRESS", "A break is already in progress.");
        }

        var brk = new Break { AttendanceRecordId = record.Id, BreakStartAt = DateTime.UtcNow };
        _db.Breaks.Add(brk);
        await _db.SaveChangesAsync(ct);

        return Result<BreakDto>.Ok(new BreakDto(brk.Id, brk.BreakStartAt, brk.BreakEndAt, brk.DurationMinutes));
    }

    public async Task<Result<BreakDto>> EndBreakAsync(Guid employeeId, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var record = await _db.AttendanceRecords.Include(a => a.Breaks)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);

        var openBreak = record?.Breaks.FirstOrDefault(b => b.BreakEndAt is null);
        if (record is null || openBreak is null)
        {
            return Result<BreakDto>.Fail("NO_ACTIVE_BREAK", "No active break to end.");
        }

        openBreak.BreakEndAt = DateTime.UtcNow;
        openBreak.DurationMinutes = (int)(openBreak.BreakEndAt.Value - openBreak.BreakStartAt).TotalMinutes;

        await _db.SaveChangesAsync(ct);
        return Result<BreakDto>.Ok(new BreakDto(openBreak.Id, openBreak.BreakStartAt, openBreak.BreakEndAt, openBreak.DurationMinutes));
    }

    public async Task<PagedResult<AttendanceRecordDto>> GetListAsync(AttendanceQuery query, CancellationToken ct)
    {
        var q = _db.AttendanceRecords.AsNoTracking().Include(a => a.Breaks).Include(a => a.Employee).AsQueryable();
        if (query.EmployeeId.HasValue) q = q.Where(a => a.EmployeeId == query.EmployeeId);
        if (query.From.HasValue) q = q.Where(a => a.Date >= query.From);
        if (query.To.HasValue) q = q.Where(a => a.Date <= query.To);
        if (query.Status.HasValue) q = q.Where(a => a.Status == query.Status);

        var total = await q.CountAsync(ct);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var records = await q.OrderByDescending(a => a.Date)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<AttendanceRecordDto> { Items = records.Select(ToDto).ToList(), Page = page, PageSize = pageSize, Total = total };
    }

    public async Task<AttendanceRecordDto?> GetTodayAsync(Guid employeeId, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var record = await _db.AttendanceRecords.Include(a => a.Breaks).Include(a => a.Employee).FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);
        return record is null ? null : ToDto(record);
    }

    // Attendance for employees who report to the given manager, per the docx's "manager view" endpoint.
    public async Task<List<AttendanceRecordDto>> GetTeamAsync(Guid managerId, DateOnly? date, CancellationToken ct)
    {
        var targetDate = date ?? await _clock.TodayAsync(ct);
        var teamEmployeeIds = await _db.Employees.AsNoTracking().Where(e => e.ManagerId == managerId).Select(e => e.Id).ToListAsync(ct);

        var records = await _db.AttendanceRecords.AsNoTracking().Include(a => a.Breaks).Include(a => a.Employee)
            .Where(a => teamEmployeeIds.Contains(a.EmployeeId) && a.Date == targetDate)
            .ToListAsync(ct);
        return records.Select(ToDto).ToList();
    }

    public async Task<Result<AttendanceRecordDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var record = await _db.AttendanceRecords.AsNoTracking().Include(a => a.Breaks).Include(a => a.Employee).FirstOrDefaultAsync(a => a.Id == id, ct);
        if (record is null) return Result<AttendanceRecordDto>.Fail("NOT_FOUND", "Attendance record not found.");
        return Result<AttendanceRecordDto>.Ok(ToDto(record));
    }

    public async Task<Result<Guid>> RequestRegularizationAsync(Guid employeeId, Guid attendanceRecordId, RegularizeRequest request, CancellationToken ct)
    {
        var record = await _db.AttendanceRecords.FirstOrDefaultAsync(a => a.Id == attendanceRecordId && a.EmployeeId == employeeId, ct);
        if (record is null) return Result<Guid>.Fail("NOT_FOUND", "Attendance record not found.");

        var regularization = new RegularizationRequest
        {
            AttendanceRecordId = record.Id,
            EmployeeId = employeeId,
            Reason = request.Reason,
            RequestedCheckInAt = request.RequestedCheckInAt,
            RequestedCheckOutAt = request.RequestedCheckOutAt,
            Status = RegularizationStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.RegularizationRequests.Add(regularization);

        record.Status = AttendanceStatus.PendingRegularization;

        await _db.SaveChangesAsync(ct);

        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee?.ManagerId is { } managerId)
        {
            await _notificationService.NotifyAsync(
                managerId, "Attendance correction awaiting approval", $"{employee.FirstName} {employee.LastName} requested an attendance correction.",
                "attendance", $"/regularizations/{regularization.Id}", ct);
        }

        return Result<Guid>.Ok(regularization.Id);
    }

    public async Task<Result<bool>> DecideRegularizationAsync(Guid regularizationId, Guid approverId, RegularizationStatus decision, string? comment, CancellationToken ct)
    {
        var regularization = await _db.RegularizationRequests
            .Include(r => r.AttendanceRecord)
            .FirstOrDefaultAsync(r => r.Id == regularizationId, ct);
        if (regularization is null) return Result<bool>.Fail("NOT_FOUND", "Regularization request not found.");
        if (regularization.Status is RegularizationStatus.Rejected or RegularizationStatus.HRApproved)
        {
            return Result<bool>.Fail("ALREADY_DECIDED", "This request has already been finalized.");
        }

        // A Manager may only decide regularization requests for their own direct reports;
        // HR/SuperAdmin can decide for anyone.
        var approver = await _db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == approverId, ct);
        var isPrivileged = RoleNames.IsPrivileged(approver?.Role.Name);
        if (!isPrivileged)
        {
            var applicant = await _db.Employees.FirstOrDefaultAsync(e => e.Id == regularization.AttendanceRecord.EmployeeId, ct);
            if (applicant?.ManagerId != approverId)
            {
                return Result<bool>.Fail("FORBIDDEN", "You can only approve or reject regularization requests for your direct reports.");
            }
        }

        regularization.Status = decision;
        _db.RegularizationApprovals.Add(new RegularizationApproval
        {
            RegularizationRequestId = regularization.Id,
            ApproverId = approverId,
            Action = decision,
            ActedAt = DateTime.UtcNow,
            Comment = comment
        });

        if (decision == RegularizationStatus.HRApproved)
        {
            if (regularization.RequestedCheckInAt.HasValue) regularization.AttendanceRecord.CheckInAt = regularization.RequestedCheckInAt;
            if (regularization.RequestedCheckOutAt.HasValue)
            {
                regularization.AttendanceRecord.CheckOutAt = regularization.RequestedCheckOutAt;
                regularization.AttendanceRecord.IsCheckedIn = false;
            }
            if (regularization.AttendanceRecord.CheckInAt.HasValue && regularization.AttendanceRecord.CheckOutAt.HasValue)
            {
                var totalBreakMinutes = await _db.Breaks
                    .Where(b => b.AttendanceRecordId == regularization.AttendanceRecordId)
                    .SumAsync(b => b.DurationMinutes ?? 0, ct);
                regularization.AttendanceRecord.WorkingMinutes = WorkingMinutesCalculator.Calculate(
                    regularization.AttendanceRecord.CheckInAt.Value, regularization.AttendanceRecord.CheckOutAt.Value, totalBreakMinutes);
            }
            regularization.AttendanceRecord.Status = AttendanceStatus.Present;
        }
        else if (decision == RegularizationStatus.Rejected)
        {
            regularization.AttendanceRecord.Status = AttendanceStatus.Absent;
        }

        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(approverId, $"regularization.{decision.ToString().ToLowerInvariant()}", "RegularizationRequest", regularization.Id.ToString(), new { comment }, ct);

        var (title, body) = decision switch
        {
            RegularizationStatus.HRApproved => ("Attendance correction approved", "Your attendance correction request was approved."),
            RegularizationStatus.Rejected => ("Attendance correction rejected", "Your attendance correction request was rejected."),
            _ => ("Attendance correction update", "Your attendance correction request status was updated."),
        };
        await _notificationService.NotifyAsync(regularization.EmployeeId, title, body, "attendance", $"/regularizations/{regularization.Id}", ct);

        return Result<bool>.Ok(true);
    }

    public async Task<bool> IsHrOrSuperAdminAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _db.Employees.Include(e => e.Role).FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        return RoleNames.IsPrivileged(employee?.Role.Name);
    }

    public async Task<string?> GetOfficeWifiSsidAsync(Guid employeeId, CancellationToken ct)
    {
        var employee = await _db.Employees.Include(e => e.Location).FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        return employee?.Location.OfficeWifiSsid;
    }

    // Powers the mobile home screen's "Today's Schedule" and "Monthly Summary" sections in
    // a single call: the employee's currently-effective shift, any in-progress break, and
    // this-month attendance counts by status plus total worked hours.
    public async Task<HomeSummaryDto> GetHomeSummaryAsync(Guid employeeId, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var now = DateTime.UtcNow;

        var currentShift = await _db.EmployeeShifts.AsNoTracking()
            .Include(es => es.Shift)
            .Where(es => es.EmployeeId == employeeId && es.EffectiveFrom <= now && (es.EffectiveTo == null || es.EffectiveTo >= now))
            .OrderByDescending(es => es.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        var todayRecord = await _db.AttendanceRecords.AsNoTracking().Include(a => a.Breaks)
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);

        var activeBreak = todayRecord?.Breaks.FirstOrDefault(b => b.BreakEndAt is null);
        var totalBreakMinutesToday = todayRecord?.Breaks.Sum(b => b.DurationMinutes ?? 0) ?? 0;

        var schedule = new TodayScheduleDto(
            currentShift?.Shift.Name,
            currentShift?.Shift.StartTime,
            currentShift?.Shift.EndTime,
            currentShift?.Shift.GracePeriodMinutes ?? 0,
            activeBreak is null ? null : new BreakDto(activeBreak.Id, activeBreak.BreakStartAt, activeBreak.BreakEndAt, activeBreak.DurationMinutes),
            totalBreakMinutesToday);

        // A single grouped aggregate query instead of pulling every attendance row for the
        // month into memory just to count/sum client-side — this endpoint is hit on every
        // mobile home-screen load.
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthAggregate = await _db.AttendanceRecords.AsNoTracking()
            .Where(a => a.EmployeeId == employeeId && a.Date >= monthStart && a.Date <= today)
            .GroupBy(a => 1)
            .Select(g => new
            {
                Present = g.Count(a => a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.HalfDay || a.Status == AttendanceStatus.WFH),
                Absent = g.Count(a => a.Status == AttendanceStatus.Absent),
                Leave = g.Count(a => a.Status == AttendanceStatus.Leave),
                Late = g.Count(a => a.Status == AttendanceStatus.Late),
                TotalWorkingMinutes = g.Sum(a => a.WorkingMinutes),
            })
            .FirstOrDefaultAsync(ct);

        var summary = new MonthlySummaryDto(
            Present: monthAggregate?.Present ?? 0,
            Absent: monthAggregate?.Absent ?? 0,
            Leave: monthAggregate?.Leave ?? 0,
            Late: monthAggregate?.Late ?? 0,
            WorkingHours: (int)Math.Round((monthAggregate?.TotalWorkingMinutes ?? 0) / 60m));

        return new HomeSummaryDto(schedule, summary);
    }

    // Requires a.Employee to be loaded (via .Include(a => a.Employee)) — every call site must
    // eager-load it, since AttendanceRecordDto now surfaces the employee's name/code for the
    // admin panel's attendance table (previously date-only, with no way to tell whose row it was).
    private static AttendanceRecordDto ToDto(AttendanceRecord a) => new(
        a.Id, a.EmployeeId, $"{a.Employee.FirstName} {a.Employee.LastName}", a.Employee.EmployeeCode, a.Date, a.CheckInAt, a.CheckOutAt, a.Status, a.WorkingMinutes,
        a.Breaks.Sum(b => b.DurationMinutes ?? 0), a.LocationId, a.CheckInMethod, a.FaceVerified, a.IsCheckedIn);
}
