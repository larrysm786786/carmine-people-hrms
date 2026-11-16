using CarminePeople.Application.Onboarding.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Onboarding.Validators;

public class CreateOnboardingTaskTemplateRequestValidator : AbstractValidator<CreateOnboardingTaskTemplateRequest>
{
    public CreateOnboardingTaskTemplateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
    }
}

public class UpdateOnboardingTaskTemplateRequestValidator : AbstractValidator<UpdateOnboardingTaskTemplateRequest>
{
    public UpdateOnboardingTaskTemplateRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
    }
}

public class CreateOnboardingTaskRequestValidator : AbstractValidator<CreateOnboardingTaskRequest>
{
    public CreateOnboardingTaskRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000).When(x => x.Description != null);
    }
}
