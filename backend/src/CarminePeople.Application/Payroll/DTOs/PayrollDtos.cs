namespace CarminePeople.Application.Payroll.DTOs;

public record CreatePayslipRequest(Guid EmployeeId, int Year, int Month, decimal GrossAmount, decimal DeductionsAmount, string? Notes);

public record PayslipDto(Guid Id, Guid EmployeeId, int Year, int Month, decimal GrossAmount, decimal DeductionsAmount, decimal NetAmount, string? Notes);

public record SetMonthlySalaryRequest(Guid EmployeeId, decimal MonthlySalary);
