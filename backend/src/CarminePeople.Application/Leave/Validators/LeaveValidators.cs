using CarminePeople.Application.Leave.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Leave.Validators;

public class ApplyLeaveRequestValidator : AbstractValidator<ApplyLeaveRequest>
{
    public ApplyLeaveRequestValidator()
    {
        RuleFor(x => x.LeaveTypeId).NotEmpty();
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate);
        RuleFor(x => x.Reason).MaximumLength(500).When(x => x.Reason != null);
    }
}

public class LeaveDecisionRequestValidator : AbstractValidator<LeaveDecisionRequest>
{
    public LeaveDecisionRequestValidator()
    {
        RuleFor(x => x.Comment).MaximumLength(500).When(x => x.Comment != null);
    }
}
