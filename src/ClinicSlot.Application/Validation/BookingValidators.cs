using ClinicSlot.Application.Contracts;
using FluentValidation;

namespace ClinicSlot.Application.Validation;

public sealed class AvailabilityRequestValidator : AbstractValidator<AvailabilityRequest>
{
    public AvailabilityRequestValidator()
    {
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).WithMessage("'To' must not be before 'From'.");
        RuleFor(x => x)
            .Must(x => x.To.DayNumber - x.From.DayNumber < AvailabilityRequest.MaxRangeDays)
            .WithName("To")
            .WithMessage($"The range may cover at most {AvailabilityRequest.MaxRangeDays} days.");
    }
}

public sealed class PatientDtoValidator : AbstractValidator<PatientDto>
{
    public PatientDtoValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.Phone).MaximumLength(32);
    }
}

public sealed class BookAppointmentRequestValidator : AbstractValidator<BookAppointmentRequest>
{
    public BookAppointmentRequestValidator()
    {
        RuleFor(x => x.PractitionerId).NotEmpty();
        RuleFor(x => x.ServiceId).NotEmpty();
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.Patient).NotNull().SetValidator(new PatientDtoValidator());
    }
}

public sealed class RescheduleAppointmentRequestValidator : AbstractValidator<RescheduleAppointmentRequest>
{
    public RescheduleAppointmentRequestValidator()
    {
        RuleFor(x => x.Start).NotEmpty();
    }
}
