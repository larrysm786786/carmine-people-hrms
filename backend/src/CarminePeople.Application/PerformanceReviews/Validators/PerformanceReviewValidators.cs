using CarminePeople.Application.PerformanceReviews.DTOs;
using FluentValidation;

namespace CarminePeople.Application.PerformanceReviews.Validators;

public class CreatePerformanceReviewRequestValidator : AbstractValidator<CreatePerformanceReviewRequest>
{
    public CreatePerformanceReviewRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
        RuleFor(x => x.Period).NotEmpty().MaximumLength(100);
    }
}

public class SubmitSelfReviewRequestValidator : AbstractValidator<SubmitSelfReviewRequest>
{
    public SubmitSelfReviewRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments != null);
    }
}

public class SubmitManagerReviewRequestValidator : AbstractValidator<SubmitManagerReviewRequest>
{
    public SubmitManagerReviewRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comments).MaximumLength(2000).When(x => x.Comments != null);
    }
}
