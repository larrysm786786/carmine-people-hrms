using CarminePeople.Application.Attendance.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Attendance.Validators;

public class CheckInRequestValidator : AbstractValidator<CheckInRequest>
{
    public CheckInRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.AccuracyMeters).GreaterThanOrEqualTo(0);
    }
}

public class CheckOutRequestValidator : AbstractValidator<CheckOutRequest>
{
    public CheckOutRequestValidator()
    {
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);
        RuleFor(x => x.AccuracyMeters).GreaterThanOrEqualTo(0);
    }
}

public class RegularizeRequestValidator : AbstractValidator<RegularizeRequest>
{
    public RegularizeRequestValidator()
    {
        RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
    }
}

public class RegularizationDecisionRequestValidator : AbstractValidator<RegularizationDecisionRequest>
{
    public RegularizationDecisionRequestValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment != null);
    }
}
