using System.Text;
using CarminePeople.Application.Attendance.Services;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Time;
using CarminePeople.Application.Leave.Services;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Ai.Services;

public record AiChatRequest(string Message, List<AiChatMessage> History);
public record AiChatResponse(string Reply);

public class AiAssistantService
{
    private readonly IApplicationDbContext _db;
    private readonly IAiClient _aiClient;
    private readonly CompanyClock _clock;
    private readonly AttendanceAnomalyService _anomalyService;
    private readonly LeavePredictionService _leavePredictionService;

    public AiAssistantService(
        IApplicationDbContext db, IAiClient aiClient, CompanyClock clock,
        AttendanceAnomalyService anomalyService, LeavePredictionService leavePredictionService)
    {
        _db = db;
        _aiClient = aiClient;
        _clock = clock;
        _anomalyService = anomalyService;
        _leavePredictionService = leavePredictionService;
    }

    public bool IsConfigured => _aiClient.IsConfigured;

    // Employee-facing: answers questions about the asking employee's own leave
    // balances, today's attendance, and upcoming holidays. Scoped to their own
    // data only — enforced below by AssertOwnedByEmployee on every fetched row, not
    // merely by the system prompt (a prompt instruction alone is not a real access
    // control — see the class-level remarks).
    public async Task<AiChatResponse> ChatAsEmployeeAsync(Guid employeeId, AiChatRequest request, CancellationToken ct)
    {
        var employee = await _db.Employees.Include(e => e.Department).Include(e => e.Designation)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null)
        {
            return new AiChatResponse("Sorry, I couldn't load your profile right now.");
        }

        var today = await _clock.TodayAsync(ct);
        var year = today.Year;
        var balances = await _db.LeaveBalances.Include(b => b.LeaveType)
            .Where(b => b.EmployeeId == employeeId && b.Year == year)
            .ToListAsync(ct);
        AssertOwnedByEmployee(balances, b => b.EmployeeId, employeeId);

        var todayAttendance = await _db.AttendanceRecords
            .FirstOrDefaultAsync(a => a.EmployeeId == employeeId && a.Date == today, ct);
        if (todayAttendance is not null) AssertOwnedByEmployee([todayAttendance], a => a.EmployeeId, employeeId);

        var upcomingHolidays = await _db.Holidays
            .Where(h => h.Date >= today && (h.LocationId == null || h.LocationId == employee.LocationId))
            .OrderBy(h => h.Date).Take(5).ToListAsync(ct);

        var context = new StringBuilder();
        context.AppendLine($"You are the HR assistant for {employee.FirstName} {employee.LastName} ({employee.Designation.Title}, {employee.Department.Name}).");
        context.AppendLine("Answer only using the data below. If asked something outside this data, say you don't have that information.");
        context.AppendLine();
        context.AppendLine("Leave balances this year:");
        foreach (var b in balances)
        {
            context.AppendLine($"- {b.LeaveType.Name}: {b.Available} available, {b.Used} used, {b.Pending} pending");
        }
        context.AppendLine();
        context.AppendLine(todayAttendance is not null
            ? $"Today's attendance: checked in at {todayAttendance.CheckInAt:HH:mm}, {(todayAttendance.CheckOutAt.HasValue ? $"checked out at {todayAttendance.CheckOutAt:HH:mm}" : "not yet checked out")}."
            : "No attendance record for today yet.");
        context.AppendLine();
        context.AppendLine("Upcoming holidays:");
        foreach (var h in upcomingHolidays)
        {
            context.AppendLine($"- {h.Name}: {h.Date:yyyy-MM-dd}{(h.IsOptional ? " (optional)" : "")}");
        }
        context.AppendLine();
        context.AppendLine("Keep answers short and friendly. You cannot approve leave, change data, or see other employees' information.");

        var history = new List<AiChatMessage>(request.History) { new("user", request.Message) };
        var reply = await _aiClient.CompleteAsync(context.ToString(), history, ct);
        return new AiChatResponse(reply);
    }

    // Defense-in-depth guard for the employee-scoped chat: throws if any row fetched for
    // "context to hand the LLM" doesn't actually belong to the requesting employee. This
    // is deliberately redundant with the .Where(EmployeeId == employeeId) filters above —
    // the point is that if a future edit ever loosens/removes one of those filters, this
    // catches the leak at runtime (fail loudly) instead of silently including another
    // employee's data in the prompt sent to the LLM. A system-prompt instruction like
    // "you cannot see other employees' data" is NOT a security boundary by itself; this is.
    private static void AssertOwnedByEmployee<T>(IEnumerable<T> rows, Func<T, Guid> employeeIdSelector, Guid expectedEmployeeId)
    {
        foreach (var row in rows)
        {
            if (employeeIdSelector(row) != expectedEmployeeId)
            {
                throw new InvalidOperationException(
                    "AI context assembly attempted to include a record belonging to a different employee than the requester. This indicates a bug in the query scoping and was blocked before reaching the LLM.");
            }
        }
    }

    // HR-facing: summarizes org-wide attendance/leave patterns for the current month.
    // Only callable by users with the audit.view permission (enforced at the controller).
    public async Task<AiChatResponse> ChatAsHrAsync(AiChatRequest request, CancellationToken ct)
    {
        var today = await _clock.TodayAsync(ct);
        var monthStart = new DateOnly(today.Year, today.Month, 1);

        var totalEmployees = await _db.Employees.CountAsync(ct);
        var onLeaveToday = await _db.LeaveApplications.CountAsync(
            l => l.Status == Domain.Enums.LeaveApplicationStatus.Approved && l.StartDate <= today && l.EndDate >= today, ct);

        var lateCheckInsThisMonth = await _db.AttendanceRecords
            .Where(a => a.Date >= monthStart && a.Date <= today && a.Status == Domain.Enums.AttendanceStatus.Late)
            .CountAsync(ct);

        var pendingRegularizations = await _db.RegularizationRequests
            .CountAsync(r => r.Status == Domain.Enums.RegularizationStatus.Pending, ct);

        var pendingLeaveApprovals = await _db.LeaveApplications
            .CountAsync(l => l.Status == Domain.Enums.LeaveApplicationStatus.Pending, ct);

        var departmentCounts = await _db.Employees
            .GroupBy(e => e.Department.Name)
            .Select(g => new { Department = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        // Feeding the same anomaly-detection and leave-forecast data the HR dashboard cards
        // show directly into the AI context, so HR can ask conversationally ("who's likely
        // to be out next month?", "any attendance concerns right now?") instead of only
        // reading it off a static card — the underlying analysis is identical either way.
        var anomalies = await _anomalyService.GetAnomaliesAsync(ct);
        var leavePredictions = await _leavePredictionService.GetUpcomingMonthPredictionsAsync(ct);

        var context = new StringBuilder();
        context.AppendLine("You are an HR analytics assistant. Answer using only the summary data below.");
        context.AppendLine($"Total employees: {totalEmployees}");
        context.AppendLine($"On approved leave today: {onLeaveToday}");
        context.AppendLine($"Late check-ins this month: {lateCheckInsThisMonth}");
        context.AppendLine($"Pending attendance regularization requests: {pendingRegularizations}");
        context.AppendLine($"Pending leave approvals: {pendingLeaveApprovals}");
        context.AppendLine("Headcount by department:");
        foreach (var d in departmentCounts)
        {
            context.AppendLine($"- {d.Department}: {d.Count}");
        }
        context.AppendLine();
        context.AppendLine(anomalies.Count == 0
            ? "Attendance anomalies: none detected — no employee's recent pattern has changed significantly."
            : "Attendance anomalies detected (employees whose recent attendance pattern changed significantly from their own baseline):");
        foreach (var a in anomalies)
        {
            context.AppendLine($"- {a.EmployeeName}: {string.Join(" ", a.Reasons)}");
        }
        context.AppendLine();
        context.AppendLine(leavePredictions.Count == 0
            ? "Leave forecast for next month: no employee shows a strong recurring pattern for this time of year yet."
            : "Employees statistically likely to take leave next month, based on their own history:");
        foreach (var p in leavePredictions)
        {
            context.AppendLine($"- {p.EmployeeName}: {p.LikelihoodPercent}% likely ({p.Reason})");
        }
        context.AppendLine();
        context.AppendLine("Give concise, actionable answers. You cannot see individual employee-level attendance/leave detail beyond what's summarized above.");

        var history = new List<AiChatMessage>(request.History) { new("user", request.Message) };
        var reply = await _aiClient.CompleteAsync(context.ToString(), history, ct);
        return new AiChatResponse(reply);
    }
}
