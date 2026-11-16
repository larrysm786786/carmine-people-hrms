using CarminePeople.Application.Signatures.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Signatures.Validators;

public class CreateSignatureDocumentRequestValidator : AbstractValidator<CreateSignatureDocumentRequest>
{
    public CreateSignatureDocumentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Content).NotEmpty().MaximumLength(10000);
    }
}

public class SubmitSignatureRequestValidator : AbstractValidator<SubmitSignatureRequest>
{
    public SubmitSignatureRequestValidator()
    {
        RuleFor(x => x.SignatureImageBase64).NotEmpty().MaximumLength(500_000);
    }
}
