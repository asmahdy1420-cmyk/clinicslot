using ClinicSlot.Application.Contracts;
using ClinicSlot.Domain.Scheduling;
using FluentValidation;

namespace ClinicSlot.Application.Validation;

public sealed class WorkingDayDtoValidator : AbstractValidator<WorkingDayDto>
{
    public WorkingDayDtoValidator()
    {
        RuleFor(x => x.Day).IsInEnum();
        RuleFor(x => x.Start).Must(SlotGrid.IsAligned).WithMessage(OnGridMessage);
        RuleFor(x => x.End)
            .GreaterThan(x => x.Start).WithMessage("'End' must be after 'Start'.")
            .Must(SlotGrid.IsAligned).WithMessage(OnGridMessage);

        RuleForEach(x => x.Breaks).ChildRules(pause =>
        {
            pause.RuleFor(b => b.Start).Must(SlotGrid.IsAligned).WithMessage(OnGridMessage);
            pause.RuleFor(b => b.End)
                .GreaterThan(b => b.Start).WithMessage("A break must end after it starts.")
                .Must(SlotGrid.IsAligned).WithMessage(OnGridMessage);
        });

        RuleFor(x => x)
            .Must(day => (day.Breaks ?? []).All(b => b.Start >= day.Start && b.End <= day.End))
            .WithName("Breaks")
            .WithMessage("Breaks must fall inside the working hours.");
    }

    private static string OnGridMessage => $"Times must be multiples of {SlotGrid.UnitMinutes} minutes.";
}

public sealed class CreatePractitionerRequestValidator : AbstractValidator<CreatePractitionerRequest>
{
    public CreatePractitionerRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.WorkingHours).NotNull().Must(HaveDistinctDays).WithMessage("Each weekday may appear only once.");
        RuleForEach(x => x.WorkingHours).SetValidator(new WorkingDayDtoValidator());
    }

    internal static bool HaveDistinctDays(IReadOnlyList<WorkingDayDto>? days) =>
        days is null || days.Select(d => d.Day).Distinct().Count() == days.Count;
}

public sealed class ReplaceScheduleRequestValidator : AbstractValidator<ReplaceScheduleRequest>
{
    public ReplaceScheduleRequestValidator()
    {
        RuleFor(x => x.WorkingHours)
            .NotNull()
            .Must(CreatePractitionerRequestValidator.HaveDistinctDays)
            .WithMessage("Each weekday may appear only once.");
        RuleForEach(x => x.WorkingHours).SetValidator(new WorkingDayDtoValidator());
    }
}

public sealed class CreateTimeOffRequestValidator : AbstractValidator<CreateTimeOffRequest>
{
    public CreateTimeOffRequestValidator()
    {
        RuleFor(x => x.Start).NotEmpty();
        RuleFor(x => x.End).GreaterThan(x => x.Start).WithMessage("'End' must be after 'Start'.");
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
