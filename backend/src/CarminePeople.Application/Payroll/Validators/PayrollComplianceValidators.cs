using CarminePeople.Application.Payroll.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Payroll.Validators;

public class UpdatePayrollComplianceSettingsRequestValidator : AbstractValidator<UpdatePayrollComplianceSettingsRequest>
{
    public UpdatePayrollComplianceSettingsRequestValidator()
    {
        RuleFor(x => x.PfEmployeeRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.PfEmployerRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.PfWageCeiling).GreaterThanOrEqualTo(0).When(x => x.PfWageCeiling != null);
        RuleFor(x => x.EsiEmployeeRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.EsiEmployerRatePercent).InclusiveBetween(0, 100);
        RuleFor(x => x.EsiEligibilityThreshold).GreaterThanOrEqualTo(0).When(x => x.EsiEligibilityThreshold != null);
    }
}

public class UpsertTdsSlabRequestValidator : AbstractValidator<UpsertTdsSlabRequest>
{
    public UpsertTdsSlabRequestValidator()
    {
        RuleFor(x => x.UpToAnnualIncome).GreaterThan(0).When(x => x.UpToAnnualIncome != null);
        RuleFor(x => x.RatePercent).InclusiveBetween(0, 100);
    }
}

public class CalculateComplianceRequestValidator : AbstractValidator<CalculateComplianceRequest>
{
    public CalculateComplianceRequestValidator()
    {
        RuleFor(x => x.MonthlyGrossAmount).GreaterThanOrEqualTo(0);
    }
}
