using ClinicSlot.Domain.Common;

namespace ClinicSlot.Domain.Practitioners;

/// <summary>A recurring weekly schedule: at most one working day entry per weekday.</summary>
public sealed class WeeklySchedule
{
    private readonly Dictionary<DayOfWeek, WorkingDay> _days = [];

    public WeeklySchedule(IEnumerable<WorkingDay> days)
    {
        ArgumentNullException.ThrowIfNull(days);

        foreach (var day in days)
        {
            if (!_days.TryAdd(day.Day, day))
            {
                throw new DomainValidationException($"{day.Day} appears more than once in the schedule.");
            }
        }
    }

    public static WeeklySchedule Empty { get; } = new([]);

    public IReadOnlyList<WorkingDay> Days => _days.Values.OrderBy(d => d.Day).ToList();

    public WorkingDay? For(DayOfWeek day) => _days.GetValueOrDefault(day);
}
