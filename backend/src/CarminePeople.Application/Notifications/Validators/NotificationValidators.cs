using CarminePeople.Application.Notifications.DTOs;
using FluentValidation;

namespace CarminePeople.Application.Notifications.Validators;

public class RegisterDeviceRequestValidator : AbstractValidator<RegisterDeviceRequest>
{
    public RegisterDeviceRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Platform).NotEmpty().Must(p => p is "android" or "ios").WithMessage("Platform must be 'android' or 'ios'.");
    }
}

public class SetPreferenceRequestValidator : AbstractValidator<SetPreferenceRequest>
{
    public SetPreferenceRequestValidator()
    {
        RuleFor(x => x.Channel).IsInEnum();
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
    }
}
