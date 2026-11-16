namespace CarminePeople.Application.Payroll.DTOs;

public record CreatePayslipRequest(Guid EmployeeId, int Year, int Month, decimal GrossAmount, decimal DeductionsAmount, string? Notes);

public record PayslipDto(Guid Id, Guid EmployeeId, int Year, int Month, decimal GrossAmount, decimal DeductionsAmount, decimal NetAmount, string? Notes);

public record SetMonthlySalaryRequest(Guid EmployeeId, decimal MonthlySalary);

public record PayrollComplianceSettingsDto(
    bool IsPfEnabled, decimal PfEmployeeRatePercent, decimal PfEmployerRatePercent, decimal? PfWageCeiling,
    bool IsEsiEnabled, decimal EsiEmployeeRatePercent, decimal EsiEmployerRatePercent, decimal? EsiEligibilityThreshold,
    bool IsTdsEnabled);

public record UpdatePayrollComplianceSettingsRequest(
    bool IsPfEnabled, decimal PfEmployeeRatePercent, decimal PfEmployerRatePercent, decimal? PfWageCeiling,
    bool IsEsiEnabled, decimal EsiEmployeeRatePercent, decimal EsiEmployerRatePercent, decimal? EsiEligibilityThreshold,
    bool IsTdsEnabled);

public record TdsSlabDto(Guid Id, decimal? UpToAnnualIncome, decimal RatePercent, int SortOrder);

public record UpsertTdsSlabRequest(decimal? UpToAnnualIncome, decimal RatePercent, int SortOrder);

public record CalculateComplianceRequest(decimal MonthlyGrossAmount);

public record ComplianceCalculationDto(
    decimal PfEmployeeAmount, decimal PfEmployerAmount,
    decimal EsiEmployeeAmount, decimal EsiEmployerAmount,
    decimal TdsAmount, decimal TotalEmployeeDeductions);
