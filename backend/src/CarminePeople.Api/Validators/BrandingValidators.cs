using FluentValidation;

namespace CarminePeople.Api.Controllers;

public class UpdateBrandingRequestValidator : AbstractValidator<UpdateBrandingRequest>
{
    private static readonly string HexColorPattern = "^#[0-9a-fA-F]{6}$";

    public UpdateBrandingRequestValidator()
    {
        RuleFor(x => x.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.AppName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ColorGray).Matches(HexColorPattern);
        RuleFor(x => x.ColorPrimary).Matches(HexColorPattern);
        RuleFor(x => x.ColorBlack).Matches(HexColorPattern);
        RuleFor(x => x.ColorWhite).Matches(HexColorPattern);
        RuleFor(x => x.GradientCss).NotEmpty();
    }
}
