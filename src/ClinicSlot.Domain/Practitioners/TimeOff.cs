using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.Domain.Practitioners;

/// <summary>A one-off block (leave, training, sickness) when a practitioner takes no bookings.</summary>
public sealed class TimeOff
{
    private TimeOff()
    {
    }

    public Guid Id { get; private set; }

    public Guid PractitionerId { get; private set; }

    public DateTimeOffset StartUtc { get; private set; }

    public DateTimeOffset EndUtc { get; private set; }

    public string? Reason { get; private set; }

    public UtcInterval Interval => new(StartUtc, EndUtc);

    public static TimeOff Create(Guid practitionerId, DateTimeOffset start, DateTimeOffset end, string? reason)
    {
        Guard.NotEmpty(practitionerId, nameof(practitionerId));
        var interval = new UtcInterval(start, end);

        return new TimeOff
        {
            Id = Guid.NewGuid(),
            PractitionerId = practitionerId,
            StartUtc = interval.Start,
            EndUtc = interval.End,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : Guard.NotBlank(reason, "Reason", 500),
        };
    }
}
