using Asp.Versioning;
using CarminePeople.Api.Common;
using CarminePeople.Application.Payroll.DTOs;
using CarminePeople.Application.Payroll.Services;
using CarminePeople.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CarminePeople.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payroll")]
[Authorize]
public class PayrollController : ControllerBase
{
    private readonly PayrollService _payrollService;
    private readonly PayrollComplianceService _complianceService;

    public PayrollController(PayrollService payrollService, PayrollComplianceService complianceService)
    {
        _payrollService = payrollService;
        _complianceService = complianceService;
    }

    [HttpPost("payslips")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> CreatePayslip([FromBody] CreatePayslipRequest request, CancellationToken ct)
    {
        var result = await _payrollService.CreateOrUpdateAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpPost("salary")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> SetMonthlySalary([FromBody] SetMonthlySalaryRequest request, CancellationToken ct)
    {
        var result = await _payrollService.SetMonthlySalaryAsync(request, User.GetEmployeeId(), ct);
        if (!result.Success) return BadRequest(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpGet("salary")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> GetMonthlySalary([FromQuery] Guid employeeId, CancellationToken ct)
    {
        var salary = await _payrollService.GetMonthlySalaryAsync(employeeId, ct);
        return Ok(new { success = true, data = new { monthlySalary = salary } });
    }

    [HttpGet("payslips")]
    [Authorize(Policy = Permissions.PayrollView)]
    public async Task<IActionResult> GetPayslips([FromQuery] Guid? employeeId, [FromQuery] int? year, CancellationToken ct)
    {
        // Self-service: an employee with only payroll.view may see their own payslips;
        // seeing anyone else's requires payroll.manage (HR/SuperAdmin).
        var targetEmployeeId = employeeId ?? User.GetEmployeeId();
        if (targetEmployeeId != User.GetEmployeeId() && !User.HasClaim("permission", Permissions.PayrollManage))
        {
            return Forbid();
        }

        var items = await _payrollService.GetForEmployeeAsync(targetEmployeeId, year, ct);
        return Ok(new { success = true, data = items });
    }

    [HttpGet("compliance/settings")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> GetComplianceSettings(CancellationToken ct)
    {
        var settings = await _complianceService.GetSettingsAsync(ct);
        return Ok(new { success = true, data = settings });
    }

    [HttpPut("compliance/settings")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> UpdateComplianceSettings([FromBody] UpdatePayrollComplianceSettingsRequest request, CancellationToken ct)
    {
        var settings = await _complianceService.UpdateSettingsAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = settings });
    }

    [HttpGet("compliance/tds-slabs")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> GetTdsSlabs(CancellationToken ct)
    {
        var slabs = await _complianceService.GetTdsSlabsAsync(ct);
        return Ok(new { success = true, data = slabs });
    }

    [HttpPost("compliance/tds-slabs")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> AddTdsSlab([FromBody] UpsertTdsSlabRequest request, CancellationToken ct)
    {
        var slab = await _complianceService.AddTdsSlabAsync(request, User.GetEmployeeId(), ct);
        return Ok(new { success = true, data = slab });
    }

    [HttpPut("compliance/tds-slabs/{id:guid}")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> UpdateTdsSlab(Guid id, [FromBody] UpsertTdsSlabRequest request, CancellationToken ct)
    {
        var result = await _complianceService.UpdateTdsSlabAsync(id, request, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = result.Data });
    }

    [HttpDelete("compliance/tds-slabs/{id:guid}")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> DeleteTdsSlab(Guid id, CancellationToken ct)
    {
        var result = await _complianceService.DeleteTdsSlabAsync(id, User.GetEmployeeId(), ct);
        if (!result.Success) return NotFound(Envelope(result.ErrorCode!, result.ErrorMessage!));
        return Ok(new { success = true, data = (object?)null });
    }

    [HttpPost("compliance/calculate")]
    [Authorize(Policy = Permissions.PayrollManage)]
    public async Task<IActionResult> CalculateCompliance([FromBody] CalculateComplianceRequest request, CancellationToken ct)
    {
        var result = await _complianceService.CalculateAsync(request, ct);
        return Ok(new { success = true, data = result });
    }

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
