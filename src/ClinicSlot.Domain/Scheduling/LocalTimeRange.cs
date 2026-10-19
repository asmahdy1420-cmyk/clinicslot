using ClinicSlot.Domain.Common;

namespace ClinicSlot.Domain.Scheduling;

/// <summary>A half-open wall-clock range [Start, End) within a single day.</summary>
public readonly record struct LocalTimeRange
{
    public LocalTimeRange(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
        {
            throw new DomainValidationException($"Time range {start:HH\\:mm}-{end:HH\\:mm} must end after it starts.");
        }

        if (!SlotGrid.IsAligned(start) || !SlotGrid.IsAligned(end))
        {
            throw new DomainValidationException($"Times must be multiples of {SlotGrid.UnitMinutes} minutes.");
        }

        Start = start;
        End = end;
    }

    public TimeOnly Start { get; }

    public TimeOnly End { get; }

    public bool Contains(LocalTimeRange other) => other.Start >= Start && other.End <= End;

    public bool Overlaps(LocalTimeRange other) => Start < other.End && other.Start < End;
}
