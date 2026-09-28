using CarminePeople.Application.Common.Interfaces;
using CarminePeople.Application.Common.Models;
using CarminePeople.Application.Notifications.Services;
using CarminePeople.Application.Payroll.DTOs;
using CarminePeople.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CarminePeople.Application.Payroll.Services;

public class PayrollService
{
    private readonly IApplicationDbContext _db;
    private readonly NotificationService _notificationService;
    private readonly IAuditLogger _auditLogger;

    public PayrollService(IApplicationDbContext db, NotificationService notificationService, IAuditLogger auditLogger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogger = auditLogger;
    }

    public async Task<Result<PayslipDto>> CreateOrUpdateAsync(CreatePayslipRequest request, Guid actorId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (employee is null) return Result<PayslipDto>.Fail("NOT_FOUND", "Employee not found.");

        var existing = await _db.Payslips.FirstOrDefaultAsync(
            p => p.EmployeeId == request.EmployeeId && p.Year == request.Year && p.Month == request.Month, ct);

        var netAmount = request.GrossAmount - request.DeductionsAmount;

        if (existing is not null)
        {
            existing.GrossAmount = request.GrossAmount;
            existing.DeductionsAmount = request.DeductionsAmount;
            existing.NetAmount = netAmount;
            existing.Notes = request.Notes;
            existing.UpdatedBy = actorId;
            await _db.SaveChangesAsync(ct);
            await _auditLogger.LogAsync(actorId, "payslip.updated", "Payslip", existing.Id.ToString(), null, ct);
            return Result<PayslipDto>.Ok(ToDto(existing));
        }

        var payslip = new Payslip
        {
            EmployeeId = request.EmployeeId,
            Year = request.Year,
            Month = request.Month,
            GrossAmount = request.GrossAmount,
            DeductionsAmount = request.DeductionsAmount,
            NetAmount = netAmount,
            Notes = request.Notes,
            CreatedBy = actorId
        };
        _db.Payslips.Add(payslip);
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "payslip.created", "Payslip", payslip.Id.ToString(), null, ct);

        await _notificationService.NotifyAsync(
            request.EmployeeId,
            "Payslip available",
            $"Your payslip for {request.Month}/{request.Year} is now available.",
            "payroll",
            $"/payroll/{payslip.Id}",
            ct);

        return Result<PayslipDto>.Ok(ToDto(payslip));
    }

    public async Task<Result<bool>> SetMonthlySalaryAsync(SetMonthlySalaryRequest request, Guid actorId, CancellationToken ct)
    {
        var employee = await _db.Employees.FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
        if (employee is null) return Result<bool>.Fail("NOT_FOUND", "Employee not found.");
        if (request.MonthlySalary < 0) return Result<bool>.Fail("INVALID_AMOUNT", "Monthly salary cannot be negative.");

        employee.MonthlySalary = request.MonthlySalary;
        employee.UpdatedBy = actorId;
        await _db.SaveChangesAsync(ct);
        await _auditLogger.LogAsync(actorId, "employee.salary_updated", "Employee", employee.Id.ToString(), null, ct);
        return Result<bool>.Ok(true);
    }

    public async Task<decimal?> GetMonthlySalaryAsync(Guid employeeId, CancellationToken ct) =>
        await _db.Employees.Where(e => e.Id == employeeId).Select(e => e.MonthlySalary).FirstOrDefaultAsync(ct);

    public async Task<List<PayslipDto>> GetForEmployeeAsync(Guid employeeId, int? year, CancellationToken ct)
    {
        var q = _db.Payslips.AsNoTracking().Where(p => p.EmployeeId == employeeId);
        if (year.HasValue) q = q.Where(p => p.Year == year);

        return await q.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month)
            .Select(p => new PayslipDto(p.Id, p.EmployeeId, p.Year, p.Month, p.GrossAmount, p.DeductionsAmount, p.NetAmount, p.Notes))
            .ToListAsync(ct);
    }

    private static PayslipDto ToDto(Payslip p) => new(p.Id, p.EmployeeId, p.Year, p.Month, p.GrossAmount, p.DeductionsAmount, p.NetAmount, p.Notes);
}
