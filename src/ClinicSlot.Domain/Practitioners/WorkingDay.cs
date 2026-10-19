using ClinicSlot.Domain.Common;
using ClinicSlot.Domain.Scheduling;

namespace ClinicSlot.Domain.Practitioners;

/// <summary>Working hours for one weekday, with optional breaks inside them.</summary>
public sealed class WorkingDay
{
    public WorkingDay(DayOfWeek day, TimeOnly start, TimeOnly end, IEnumerable<LocalTimeRange>? breaks = null)
    {
        if (!Enum.IsDefined(day))
        {
            throw new DomainValidationException($"Unknown day of week '{day}'.");
        }

        Day = day;
        Hours = new LocalTimeRange(start, end);

        var sorted = (breaks ?? []).OrderBy(b => b.Start).ToList();
        for (var i = 0; i < sorted.Count; i++)
        {
            if (!Hours.Contains(sorted[i]))
            {
                throw new DomainValidationException($"Breaks on {day} must fall inside the working hours.");
            }

            if (i > 0 && sorted[i - 1].Overlaps(sorted[i]))
            {
                throw new DomainValidationException($"Breaks on {day} must not overlap.");
            }
        }

        Breaks = sorted;
    }

    public DayOfWeek Day { get; }

    public LocalTimeRange Hours { get; }

    public IReadOnlyList<LocalTimeRange> Breaks { get; }

    /// <summary>The working hours with the breaks removed, in chronological order.</summary>
    public IReadOnlyList<LocalTimeRange> WorkingWindows()
    {
        var windows = new List<LocalTimeRange>();
        var cursor = Hours.Start;

        foreach (var pause in Breaks)
        {
            if (pause.Start > cursor)
            {
                windows.Add(new LocalTimeRange(cursor, pause.Start));
            }

            cursor = pause.End;
        }

        if (cursor < Hours.End)
        {
            windows.Add(new LocalTimeRange(cursor, Hours.End));
        }

        return windows;
    }
}
