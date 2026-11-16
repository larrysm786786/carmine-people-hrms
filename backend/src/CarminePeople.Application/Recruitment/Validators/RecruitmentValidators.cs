using CarminePeople.Application.Recruitment.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Recruitment.Validators;

public class CreateJobPostingRequestValidator : AbstractValidator<CreateJobPostingRequest>
{
    public CreateJobPostingRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Department).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description != null);
    }
}

public class CreateCandidateRequestValidator : AbstractValidator<CreateCandidateRequest>
{
    public CreateCandidateRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(200);
        RuleFor(x => x.Phone).MaximumLength(30).When(x => x.Phone != null);
        RuleFor(x => x.ResumeUrl).MaximumLength(500).When(x => x.ResumeUrl != null);
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}

public class UpdateCandidateStageRequestValidator : AbstractValidator<UpdateCandidateStageRequest>
{
    public UpdateCandidateStageRequestValidator()
    {
        RuleFor(x => x.Stage).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(2000).When(x => x.Notes != null);
    }
}
