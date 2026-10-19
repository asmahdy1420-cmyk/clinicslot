using ClinicSlot.Domain.Common;

namespace ClinicSlot.Domain.Scheduling;

/// <summary>A half-open interval [Start, End) on the UTC time line.</summary>
public readonly record struct UtcInterval
{
    public UtcInterval(DateTimeOffset start, DateTimeOffset end)
    {
        if (end <= start)
        {
            throw new DomainValidationException("An interval must end after it starts.");
        }

        Start = start.ToUniversalTime();
        End = end.ToUniversalTime();
    }

    public DateTimeOffset Start { get; }

    public DateTimeOffset End { get; }

    public TimeSpan Length => End - Start;

    public bool Overlaps(UtcInterval other) => Start < other.End && other.Start < End;
}
