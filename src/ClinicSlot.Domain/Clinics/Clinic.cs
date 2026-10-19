using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.Domain.Clinics;

public sealed class Clinic
{
    public const int MinSlotIntervalMinutes = SlotGrid.UnitMinutes;
    public const int MaxSlotIntervalMinutes = 120;
    public const int MaxChangeCutoffHours = 168;

    private Clinic()
    {
        Name = string.Empty;
        TimeZoneId = string.Empty;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    /// <summary>IANA (or Windows) time zone id, e.g. "Europe/London".</summary>
    public string TimeZoneId { get; private set; }

    /// <summary>Distance between offered start times, e.g. every 15 minutes.</summary>
    public int SlotIntervalMinutes { get; private set; }

    /// <summary>Appointments cannot be cancelled or rescheduled within this many hours of their start.</summary>
    public int ChangeCutoffHours { get; private set; }

    public TimeSpan SlotInterval => TimeSpan.FromMinutes(SlotIntervalMinutes);

    public TimeSpan ChangeCutoff => TimeSpan.FromHours(ChangeCutoffHours);

    public static Clinic Create(string name, string timeZoneId, int slotIntervalMinutes, int changeCutoffHours)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId) || !TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _))
        {
            throw new DomainValidationException($"Unknown time zone '{timeZoneId}'.");
        }

        if (slotIntervalMinutes is < MinSlotIntervalMinutes or > MaxSlotIntervalMinutes
            || !SlotGrid.IsAligned(TimeSpan.FromMinutes(slotIntervalMinutes)))
        {
            throw new DomainValidationException(
                $"Slot interval must be between {MinSlotIntervalMinutes} and {MaxSlotIntervalMinutes} minutes and a multiple of {SlotGrid.UnitMinutes}.");
        }

        if (changeCutoffHours is < 0 or > MaxChangeCutoffHours)
        {
            throw new DomainValidationException($"Change cutoff must be between 0 and {MaxChangeCutoffHours} hours.");
        }

        return new Clinic
        {
            Id = Guid.NewGuid(),
            Name = Guard.NotBlank(name, "Name"),
            TimeZoneId = timeZoneId.Trim(),
            SlotIntervalMinutes = slotIntervalMinutes,
            ChangeCutoffHours = changeCutoffHours,
        };
    }

    public TimeZoneInfo GetTimeZone() => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
}
