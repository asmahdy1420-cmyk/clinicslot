using ClinicSlot.Domain.Common;

namespace ClinicSlot.Domain.Practitioners;

public sealed class Practitioner
{
    private Practitioner()
    {
        Name = string.Empty;
        Schedule = WeeklySchedule.Empty;
    }

    public Guid Id { get; private set; }

    public Guid ClinicId { get; private set; }

    public string Name { get; private set; }

    public WeeklySchedule Schedule { get; private set; }

    public static Practitioner Create(Guid clinicId, string name, WeeklySchedule schedule)
    {
        Guard.NotEmpty(clinicId, nameof(clinicId));
        ArgumentNullException.ThrowIfNull(schedule);

        return new Practitioner
        {
            Id = Guid.NewGuid(),
            ClinicId = clinicId,
            Name = Guard.NotBlank(name, "Name"),
            Schedule = schedule,
        };
    }

    public void ReplaceSchedule(WeeklySchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);
        Schedule = schedule;
    }
}
