using CarminePeople.Application.Assets.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Assets.Validators;

public class CreateAssetRequestValidator : AbstractValidator<CreateAssetRequest>
{
    public CreateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100).When(x => x.SerialNumber != null);
    }
}

public class UpdateAssetRequestValidator : AbstractValidator<UpdateAssetRequest>
{
    public UpdateAssetRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SerialNumber).MaximumLength(100).When(x => x.SerialNumber != null);
        RuleFor(x => x.Status).IsInEnum();
    }
}

public class AssignAssetRequestValidator : AbstractValidator<AssignAssetRequest>
{
    public AssignAssetRequestValidator()
    {
        RuleFor(x => x.EmployeeId).NotEmpty();
    }
}
