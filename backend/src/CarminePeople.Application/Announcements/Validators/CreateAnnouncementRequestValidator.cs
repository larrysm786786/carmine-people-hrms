using CarminePeople.Application.Announcements.DTOs;
using CarminePeople.Domain.Enums;
using FluentValidation;

namespace CarminePeople.Application.Announcements.Validators;

public class CreateAnnouncementRequestValidator : AbstractValidator<CreateAnnouncementRequest>
{
    public CreateAnnouncementRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty().MaximumLength(5000);
        RuleFor(x => x.PublishAt).NotEmpty();
        RuleFor(x => x.AudienceIds)
            .Must(ids => ids != null && ids.Count > 0)
            .When(x => x.AudienceType != AnnouncementAudienceType.All)
            .WithMessage("AudienceIds is required when AudienceType is not 'All'.");
    }
}
