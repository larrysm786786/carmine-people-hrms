using Asp.Versioning;
using CarminePeople.Domain.Constants;
using CarminePeople.Api.Common;
using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Documents.Services;
using CarminePeople.Application.Reports.DTOs;
using CarminePeople.Application.Reports.Services;
using CarminePeople.Domain.Enums;
using CarminePeople.Infrastructure.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports")]
[Authorize(Policy = Permissions.AttendanceView)]
public class ReportsController : ControllerBase
{
    private readonly ReportService _reportService;
    private readonly IApplicationDbContext _db;
    private readonly IEmailSender _emailSender;
    private readonly DocumentService _documentService;

    public ReportsController(ReportService reportService, IApplicationDbContext db, IEmailSender emailSender, DocumentService documentService)
    {
        _db = db;
        _emailSender = emailSender;
        _documentService = documentService;
        _reportService = reportService;
    }

    // Managers only ever see reports scoped to their own direct reports, regardless of what
    // filters they pass; HR/SuperAdmin see company-wide data via employee.view/leave.view etc.
    private ReportQuery Scoped(ReportQuery query) =>
        User.IsInRole("Manager") ? query with { ManagerId = User.GetEmployeeId() } : query;

    [HttpGet("attendance")]
    public async Task<IActionResult> Attendance([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var rows = await _reportService.GetAttendanceReportAsync(Scoped(query), ct);
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("attendance/export")]
    public async Task<IActionResult> AttendanceExport([FromQuery] ReportQuery query, [FromQuery] string format, CancellationToken ct)
    {
        var rows = await _reportService.GetAttendanceReportAsync(Scoped(query), ct);
        return ExportCsvOnly(rows, format, "attendance-report");
    }

    [HttpGet("leave")]
    [Authorize(Policy = Permissions.LeaveView)]
    public async Task<IActionResult> Leave([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var rows = await _reportService.GetLeaveReportAsync(Scoped(query), ct);
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("leave/export")]
    [Authorize(Policy = Permissions.LeaveView)]
    public async Task<IActionResult> LeaveExport([FromQuery] ReportQuery query, [FromQuery] string format, CancellationToken ct)
    {
        var rows = await _reportService.GetLeaveReportAsync(Scoped(query), ct);
        return ExportCsvOnly(rows, format, "leave-report");
    }

    [HttpGet("employee")]
    [Authorize(Policy = Permissions.EmployeeView)]
    public async Task<IActionResult> Employee([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var rows = await _reportService.GetEmployeeReportAsync(Scoped(query), ct);
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("employee/export")]
    [Authorize(Policy = Permissions.EmployeeView)]
    public async Task<IActionResult> EmployeeExport([FromQuery] ReportQuery query, [FromQuery] string format, CancellationToken ct)
    {
        var rows = await _reportService.GetEmployeeReportAsync(Scoped(query), ct);
        return ExportCsvOnly(rows, format, "employee-report");
    }

    [HttpGet("late")]
    public async Task<IActionResult> Late([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var rows = await _reportService.GetLateReportAsync(Scoped(query), ct);
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("late/export")]
    public async Task<IActionResult> LateExport([FromQuery] ReportQuery query, [FromQuery] string format, CancellationToken ct)
    {
        var rows = await _reportService.GetLateReportAsync(Scoped(query), ct);
        return ExportCsvOnly(rows, format, "late-report");
    }

    [HttpGet("working-hours")]
    public async Task<IActionResult> WorkingHours([FromQuery] ReportQuery query, CancellationToken ct)
    {
        var rows = await _reportService.GetWorkingHoursReportAsync(Scoped(query), ct);
        return Ok(new { success = true, data = rows });
    }

    [HttpGet("working-hours/export")]
    public async Task<IActionResult> WorkingHoursExport([FromQuery] ReportQuery query, [FromQuery] string format, CancellationToken ct)
    {
        var rows = await _reportService.GetWorkingHoursReportAsync(Scoped(query), ct);
        return ExportCsvOnly(rows, format, "working-hours-report");
    }

    // Sends one employee's day-by-day attendance grid PDF (the classic biometric-machine
    // report layout) to their email immediately — the same PDF the automated monthly job
    // attaches, exposed here so HR/Admin can trigger a one-off resend/test without waiting
    // for month-end.
    [HttpPost("attendance/send-pdf")]
    [Authorize(Policy = Permissions.AttendanceView)]
    public async Task<IActionResult> SendAttendancePdf([FromQuery] Guid employeeId, [FromQuery] int year, [FromQuery] int month, CancellationToken ct)
    {
        var employee = await _db.Employees.Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Id == employeeId, ct);
        if (employee is null) return NotFound(new { success = false, error = new { code = "NOT_FOUND", message = "Employee not found." } });

        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        var records = await _db.AttendanceRecords
            .Where(a => a.EmployeeId == employeeId && a.Date >= monthStart && a.Date <= monthEnd)
            .Select(a => new { a.Date, a.Status, a.CheckInAt, a.CheckOutAt, a.WorkingMinutes })
            .ToListAsync(ct);
        var recordsByDate = records.ToDictionary(r => r.Date);

        var days = new List<AttendanceGridDay>();
        for (var date = monthStart; date <= monthEnd; date = date.AddDays(1))
        {
            if (recordsByDate.TryGetValue(date, out var r))
            {
                days.Add(new AttendanceGridDay(
                    date,
                    AttendanceStatusCode.For(r.Status),
                    r.CheckInAt.HasValue ? TimeOnly.FromDateTime(r.CheckInAt.Value) : null,
                    r.CheckOutAt.HasValue ? TimeOnly.FromDateTime(r.CheckOutAt.Value) : null,
                    r.WorkingMinutes));
            }
            else
            {
                days.Add(new AttendanceGridDay(date, null, null, null, 0));
            }
        }

        var monthLabel = monthStart.ToString("MMMM yyyy");
        var pdfBytes = AttendanceGridPdfGenerator.Generate(new AttendanceGridReportData(
            "Carmine Communications LLP", employee.EmployeeCode, $"{employee.FirstName} {employee.LastName}",
            employee.Department.Name, monthStart, monthEnd, days));
        var pdfFileName = $"Attendance-{monthLabel.Replace(' ', '-')}.pdf";

        using (var pdfStream = new MemoryStream(pdfBytes))
        {
            await _documentService.UploadAsync(
                employee.Id, employee.Id, DocumentType.AttendanceReport,
                pdfFileName, "application/pdf", pdfBytes.Length, pdfStream, ct);
        }

        var emailSent = false;
        if (!string.IsNullOrWhiteSpace(employee.Email))
        {
            var html = $"""
                <p>Hi {employee.FirstName},</p>
                <p>Here is your attendance summary for <strong>{monthLabel}</strong>. Your full day-by-day attendance is attached as a PDF, and is also saved under Documents in the app.</p>
                <p>— Carmine People</p>
                """;
            await _emailSender.SendAsync(
                employee.Email,
                $"Your attendance summary — {monthLabel}",
                html,
                ct,
                [new EmailAttachment(pdfFileName, pdfBytes, "application/pdf")]);
            emailSent = true;
        }

        return Ok(new { success = true, data = new { sentTo = emailSent ? employee.Email : null, savedToDocuments = true, month = monthLabel, dayCount = days.Count } });
    }

    // PDF is not implemented; CSV and xlsx cover the two formats admins actually use for reports.
    private IActionResult ExportCsvOnly<T>(IEnumerable<T> rows, string format, string fileNameBase)
    {
        var rowList = rows.ToList();
        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var csvBytes = CsvExporter.ToCsv(rowList);
            return File(csvBytes, "text/csv", $"{fileNameBase}.csv");
        }
        if (string.Equals(format, "xlsx", StringComparison.OrdinalIgnoreCase))
        {
            var xlsxBytes = XlsxExporter.ToXlsx(rowList);
            return File(xlsxBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{fileNameBase}.xlsx");
        }
        return BadRequest(new { success = false, error = new { code = "UNSUPPORTED_FORMAT", message = "Supported export formats: csv, xlsx." } });
    }
}
