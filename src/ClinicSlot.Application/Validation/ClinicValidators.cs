using ClinicSlot.Application.Contracts;
using ClinicSlot.Domain.Clinics;
using ClinicSlot.Domain.Scheduling;
using FluentValidation;

namespace ClinicSlot.Application.Validation;

public sealed class CreateClinicRequestValidator : AbstractValidator<CreateClinicRequest>
{
    public CreateClinicRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TimeZone)
            .NotEmpty()
            .Must(id => TimeZoneInfo.TryFindSystemTimeZoneById(id, out _))
            .WithMessage("'{PropertyValue}' is not a known time zone id (use an IANA id such as Europe/London).");
        RuleFor(x => x.SlotIntervalMinutes)
            .InclusiveBetween(Clinic.MinSlotIntervalMinutes, Clinic.MaxSlotIntervalMinutes)
            .Must(BeOnGrid).WithMessage($"Slot interval must be a multiple of {SlotGrid.UnitMinutes} minutes.");
        RuleFor(x => x.ChangeCutoffHours).InclusiveBetween(0, Clinic.MaxChangeCutoffHours);
    }

    internal static bool BeOnGrid(int minutes) => minutes % SlotGrid.UnitMinutes == 0;
}

public sealed class CreateServiceRequestValidator : AbstractValidator<CreateServiceRequest>
{
    public CreateServiceRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(SlotGrid.UnitMinutes, ClinicService.MaxDurationMinutes)
            .Must(CreateClinicRequestValidator.BeOnGrid)
            .WithMessage($"Duration must be a multiple of {SlotGrid.UnitMinutes} minutes.");
        RuleFor(x => x.BufferMinutes)
            .InclusiveBetween(0, ClinicService.MaxBufferMinutes)
            .Must(CreateClinicRequestValidator.BeOnGrid)
            .WithMessage($"Buffer must be a multiple of {SlotGrid.UnitMinutes} minutes.");
    }
}
