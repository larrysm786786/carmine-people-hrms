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

    public PayrollController(PayrollService payrollService)
    {
        _payrollService = payrollService;
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

    private object Envelope(string code, string message) => ApiEnvelope.Error(HttpContext, code, message);
}
