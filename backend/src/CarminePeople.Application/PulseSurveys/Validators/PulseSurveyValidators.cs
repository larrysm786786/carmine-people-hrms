using CarminePeople.Application.PulseSurveys.DTOs;
using FluentValidation;

namespace CarminePeople.Application.PulseSurveys.Validators;

public class CreatePulseSurveyRequestValidator : AbstractValidator<CreatePulseSurveyRequest>
{
    public CreatePulseSurveyRequestValidator()
    {
        RuleFor(x => x.Question).NotEmpty().MaximumLength(500);
    }
}

public class SubmitPulseSurveyResponseRequestValidator : AbstractValidator<SubmitPulseSurveyResponseRequest>
{
    public SubmitPulseSurveyResponseRequestValidator()
    {
        RuleFor(x => x.Rating).InclusiveBetween(1, 5);
        RuleFor(x => x.Comment).MaximumLength(1000).When(x => x.Comment != null);
    }
}
